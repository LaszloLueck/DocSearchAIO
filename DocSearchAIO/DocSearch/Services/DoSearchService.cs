using System.Diagnostics;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Endpoints.Search;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services;

public interface IDoSearchService
{
    public Task<DoSearchResponse> DoSearch(DoSearchRequest doSearchRequest);
}

public class DoSearchService : IDoSearchService
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
                    {"content", new HighlightField()}, {"comments.comment", new HighlightField()}
                },
                Fragmenter = HighlighterFragmenter.Span,
                FragmentSize = 500,
                NumberOfFragments = 5,
                Encoder = HighlighterEncoder.Html
            };

            var io = new[] { new Field("completionContent") };

            var indicesResponse =
                await _elasticSearchService.IndicesWithPatternAsync($"{_configurationObject.IndexName}-*");
            var knownIndices = indicesResponse.Indices.Keys.Map(index => index.Name);

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
            var enumerable = Some(knownIndices)
                .IfNone(System.Array.Empty<string>())
                .ToArray();

            documentTypesAndFilters.ForEach((filterType, requestFilter) =>
            {
                if (StaticHelpers.TypedIndexKeyExistsAndFilter(filterType, _configurationObject, enumerable,
                        requestFilter))
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
                Query = new QueryContainer(query),
                Highlight = highlight,
                From = from,
                Size = size,
                Source = f
            };

            var sw = Stopwatch.StartNew();
            var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
            sw.Stop();
            _logger.LogInformation("find {ResultTotal} documents in {ElapsedTimeMs} ms", result.Total,
                sw.ElapsedMilliseconds);

            var paginationResult = new DoSearchResult(from, size, result.Total, searchPhrase);
            var statisticsModel = new SearchStatisticsModel(sw.ElapsedMilliseconds, result.Total);

            var retCol = result.Hits.Map(hit =>
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

                IEnumerable<ContentDetail> highlightContent = System.Array.Empty<ContentDetail>();
                IEnumerable<CommentDetail> highlightComments = System.Array.Empty<CommentDetail>();

                if (hit.Highlight.ContainsKey("content"))
                {
                    highlightContent = hit.Highlight["content"].Map(p =>
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

                if (!hit.Highlight.ContainsKey("comments.comment"))
                    return new DoSearchResultContainer(hit.Source.UriFilePath, hit.Source.Id,
                        hit.Source.ProcessTime, hit.Source.OriginalFilePath, hit.Source.ContentType, hit.Index)
                    {
                        Contents = highlightContent,
                        Comments = highlightComments,
                        Relevance = hit.Score.IfNone(0d),
                        ProgramIcon = IconType(hit.Source.ContentType)
                    };
                {
                    var originalComments = hit.Source.Comments;

                    highlightComments = hit.Highlight["comments.comment"].Map(p =>
                    {
                        var prepText = p.Replace(highlight.PreTags.First(), "").Replace(highlight.PostTags.First(), "");
                        var commentObj = originalComments.Filter(c => c.Comment.Contains(prepText)).ToOption();

                        var retVal = commentObj.IsSome
                            ? new CommentDetail(p)
                            {
                                Author = commentObj.ValueUnsafe().Author,
                                Date = commentObj.ValueUnsafe().Date,
                                Id = commentObj.ValueUnsafe().Id,
                                Initials = commentObj.ValueUnsafe().Initials
                            }
                            : new CommentDetail(p);


                        return retVal;
                    });
                }

                return new DoSearchResultContainer(hit.Source.UriFilePath, hit.Source.Id,
                    hit.Source.ProcessTime, hit.Source.OriginalFilePath, hit.Source.ContentType, hit.Index)
                {
                    Contents = highlightContent,
                    Comments = highlightComments,
                    Relevance = hit.Score.IfNone(0d),
                    ProgramIcon = IconType(hit.Source.ContentType)
                };
            });

            return new DoSearchResponse(retCol, paginationResult, statisticsModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured");
            return new DoSearchResponse(System.Array.Empty<DoSearchResultContainer>(), new DoSearchResult(0, 0, 0, ""),
                new SearchStatisticsModel(0, 0));
        }
    }
}