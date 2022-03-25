using System.Diagnostics;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services
{
    public class DoSearchService
    {
        private readonly ILogger<DoSearchService> _logger;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ConfigurationObject _configurationObject;

        public DoSearchService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<DoSearchService>();
            _elasticSearchService = elasticSearchService;
            _configurationObject = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_configurationObject);
        }

#pragma warning disable S3776
        public async Task<DoSearchResponse> DoSearch(DoSearchRequest doSearchRequest)
        {
            try
            {
                var from = doSearchRequest.From;
                var size = doSearchRequest.Size;
                var searchPhrase = CheckSearchPhrase(doSearchRequest.SearchPhrase);
                var query = new SimpleQueryStringQuery();
                var include = new[] { new Field("comments.comment"), new Field("content") };
                query.Fields = include;
                query.Query = searchPhrase;
                query.AnalyzeWildcard = true;

                static string CheckSearchPhrase(string searchPhrase)
                {
                    return searchPhrase == "" ? "*" : searchPhrase;
                }

                var highlight = new Highlight
                {
                    PreTags = new[] { @"<span class=""hilightText""><strong>" },
                    PostTags = new[] { "</strong></span>" },
                    Fields = new Dictionary<Field, IHighlightField>
                    {
                        { "content", new HighlightField() }, { "comments.comment", new HighlightField() }
                    },
                    Fragmenter = HighlighterFragmenter.Span,
                    FragmentSize = 500,
                    NumberOfFragments = 5,
                    Encoder = HighlighterEncoder.Html
                };

                var io = new[] { new Field("completionContent") };

                var indicesResponse =
                    await _elasticSearchService.IndicesWithPatternAsync($"{_configurationObject.IndexName}-*");
                var knownIndices = indicesResponse.Indices.Keys.Select(index => index.Name);

                var documentTypesAndFilters = new List<(Type, bool)>
                {
                    (typeof(WordElasticDocument), doSearchRequest.FilterWord),
                    (typeof(ExcelElasticDocument), doSearchRequest.FilterExcel),
                    (typeof(PowerpointElasticDocument), doSearchRequest.FilterPowerpoint),
                    (typeof(PdfElasticDocument), doSearchRequest.FilterPdf),
                    (typeof(MsgElasticDocument), doSearchRequest.FilterMsg),
                    (typeof(EmlElasticDocument), doSearchRequest.FilterEml)
                };
                
                var selectedIndices = new List<string>();
                var enumerable = knownIndices.ResolveNullable(Array.Empty<string>(), (v, _) => v.ToArray());
                documentTypesAndFilters.ForEach((filterType, requestFilter) => 
                {
                    if(StaticHelpers.TypedIndexKeyExistsAndFilter(filterType,_configurationObject, enumerable, requestFilter))
                        selectedIndices.Add(StaticHelpers.IndexNameByType(filterType, _configurationObject));
                });

                if (selectedIndices.Count == 6)
                {
                    selectedIndices.Clear();
                    selectedIndices.Add($"{_configurationObject.IndexName}-*");
                }

                var indices = Indices.Index(selectedIndices);

                var f = new SourceFilter { Excludes = io };

                var request = new SearchRequest(indices)
                {
                    Query = new QueryContainer(query), Highlight = highlight, From = from, Size = size, Source = f
                };

                var sw = Stopwatch.StartNew();
                var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
                sw.Stop();
                _logger.LogInformation("find {ResultTotal} documents in {ElapsedTimeMs} ms", result.Total,
                    sw.ElapsedMilliseconds);

                var paginationResult = new DoSearchResult(from, size, result.Total, searchPhrase);
                var statisticsModel = new SearchStatisticsModel(sw.ElapsedMilliseconds, result.Total);

                var retCol = result.Hits.Select(hit =>
                {
                    static string IconType(string contentType) => contentType switch
                    {
                        "docx" => "./images/word.svg",
                        "pptx" => "./images/powerpoint.svg",
                        "xlsx" => "./images/excel.svg",
                        "pdf" => "./images/pdf.svg",
                        "msg" => "./images/outlook.svg",
                        "eml" => "./images/eml.svg",
                        _ => "./images/unknown.svg"
                    };

                    IEnumerable<ContentDetail> highlightContent = Array.Empty<ContentDetail>();
                    IEnumerable<CommentDetail> highlightComments = Array.Empty<CommentDetail>();

                    if (hit.Highlight.ContainsKey("content"))
                    {
                        highlightContent = hit.Highlight["content"].Select(p =>
                            new ContentDetail(p));
                    }
                    else
                    {
                        highlightContent = hit.Source.Content.Length > 512
                            ? new List<ContentDetail>
                            {
                                new(hit.Source.Content[..512] + " ...")
                            }
                            : new List<ContentDetail> { new(hit.Source.Content) };
                    }

                    if (hit.Highlight.ContainsKey("comments.comment"))
                    {
                        var originalComments = hit.Source.Comments;

                        highlightComments = hit.Highlight["comments.comment"].Select(p =>
                        {
                            var prepText = p.Replace(highlight.PreTags.First(), "").Replace(highlight.PostTags.First(), "");
                            var commentObj = originalComments.Where(c => c.Comment.Contains(prepText)).TryFirst();

                            var retVal = commentObj.HasValue
                                ? new CommentDetail(p)
                                {
                                    Author = commentObj.Value.Author, Date = commentObj.Value.Date, Id = commentObj.Value.Id,
                                    Initials = commentObj.Value.Initials
                                }
                                : new CommentDetail(p);


                            return retVal;
                        });
                    }

                    DoSearchResultContainer container = hit.Source;
                    container.Contents = highlightContent;
                    container.Comments = highlightComments;
                    container.Relevance = hit.Score.ResolveNullable(0d, (v, a) => v ?? a);
                    container.ProgramIcon = IconType(hit.Source.ContentType);
                    return container;
                });

                return new DoSearchResponse(retCol, paginationResult, statisticsModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                return new DoSearchResponse(Array.Empty<DoSearchResultContainer>(), new DoSearchResult(0, 0, 0, ""), new SearchStatisticsModel(0, 0));
            }
        }
#pragma warning restore S3776
    }
}