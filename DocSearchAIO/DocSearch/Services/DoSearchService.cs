using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services
{
    public class DoSearchService
    {
        private readonly ILogger<DoSearchService> _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ConfigurationObject _configurationObject;
        
        public DoSearchService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory,
            ViewToStringRenderer viewToStringRenderer, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<DoSearchService>();
            _viewToStringRenderer = viewToStringRenderer;
            _elasticSearchService = elasticSearchService;
            var cfgTmp = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
        }


        public async Task<DoSearchResponse> DoSearch(DoSearchRequest doSearchRequest)
        {
            try
            {
                var from = doSearchRequest.From ?? 0;
                var size = doSearchRequest.Size ?? 10;
                var searchPhrase = doSearchRequest.SearchPhrase == "" ? "*" : doSearchRequest.SearchPhrase;
                var query = new SimpleQueryStringQuery();
                var include = new[] {new Field("comments.comment"), new Field("content")};
                query.Fields = include;
                query.Query = searchPhrase;
                query.AnalyzeWildcard = true;

                var highlight = new Highlight
                {
                    PreTags = new[] {"[#OO#]"},
                    PostTags = new[] {"[#CO#]"},
                    Fields = new Dictionary<Field, IHighlightField>()
                    {
                        {"content", new HighlightField()}, {"comments.comment", new HighlightField()}
                    },
                    Fragmenter = HighlighterFragmenter.Span,
                    FragmentSize = 500,
                    NumberOfFragments = 5,
                    Encoder = HighlighterEncoder.Html
                };

                Field[] io = new[] {new Field("completionContent")};

                var indicesResponse = await _elasticSearchService.GetIndicesWithPatternAsync($"{_configurationObject.IndexName}-*");
                var knownIndices = indicesResponse.Indices.Keys.Select(index => index.Name);


                var selectedIndices = new List<string>();
                if (knownIndices.Contains("officedocuments-word") && doSearchRequest.FilterWord)
                    selectedIndices.Add("officedocuments-word");
                if (knownIndices.Contains("officedocuments-excel") && doSearchRequest.FilterExcel)
                    selectedIndices.Add("officedocuments-excel");
                if (knownIndices.Contains("officedocuments-powerpoint") && doSearchRequest.FilterPowerpoint)
                    selectedIndices.Add("officedocuments-powerpoint");
                if (knownIndices.Contains("officedocuments-pdf") && doSearchRequest.FilterPdf)
                    selectedIndices.Add("officedocuments-pdf");

                if (selectedIndices.Count == 4)
                {
                    selectedIndices.Clear();
                    selectedIndices.Add("officedocuments-*");
                }

                var indices = Indices.Index(selectedIndices);

                var f = new SourceFilter {Excludes = io};

                var request = new SearchRequest(indices)
                {
                    Query = new QueryContainer(query), Highlight = highlight, From = @from, Size = size, Source = f
                };

                var sw = Stopwatch.StartNew();
                var result = await _elasticSearchService.SearchIndexAsync<WordElasticDocument>(request);
                sw.Stop();
                _logger.LogInformation($"find {result.Total} documents in {sw.ElapsedMilliseconds} ms");

                var paginationResult = new DoSearchResult
                {
                    DocCount = result.Total,
                    SearchPhrase = searchPhrase,
                    CurrentPage = @from,
                    CurrentPageSize = size
                };

                var outResponse = new DoSearchResponse();

                var pagination = await _viewToStringRenderer.Render("PaginationPartial", paginationResult);
                outResponse.SearchPhrase = searchPhrase;
                outResponse.Pagination = pagination;

                var statisticsModel = new SearchStatisticsModel()
                    {DocCount = result.Total, SearchTime = sw.ElapsedMilliseconds};
                var statisticResponse = await _viewToStringRenderer.Render("SearchStatisticsPartial", statisticsModel);
                outResponse.Statistics = statisticResponse;

                var retCol = result.Hits.Select(hit =>
                {
                    var iconType = hit.Source.ContentType switch
                    {
                        "docx" => "./images/word.svg",
                        "pptx" => "./images/powerpoint.svg",
                        "excel" => "./images/excel.svg",
                        "pdf" => "./images/pdf.svg",
                        _ => "./images/unknown.svg"
                    };

                    var highlightContent = new List<Tuple<string, string>>();
                    var highlightComments = new List<Tuple<string, string>>();

                    if (hit.Highlight.ContainsKey("content"))
                    {
                        highlightContent = hit.Highlight["content"].Select(p =>
                                Tuple.Create("Inhalt", p))
                            .ToList();
                    }
                    else
                    {
                        highlightContent.Add(hit.Source.Content.Length > 512
                            ? Tuple.Create("Inhalt", hit.Source.Content[..512] + " ...")
                            : Tuple.Create("Inhalt", hit.Source.Content));
                    }

                    if (hit.Highlight.ContainsKey("comments.comment"))
                    {
                        highlightComments = hit.Highlight["comments.comment"].Select(p =>
                                Tuple.Create("Kommentar", p))
                            .ToList();
                    }

                    var grouped = highlightContent.Concat(highlightComments).GroupBy(item => item.Item1).Select(o =>
                        new ContentTypeAndValues()
                            {ContentType = o.Key, ContentValues = o.Select(s => s.Item2)});

                    return new DoSearchResultContainer()
                    {
                        RelativeUrl = hit.Source.UriFilePath,
                        Relevance = hit.Score ?? 0,
                        SearchBody = grouped,
                        Id = hit.Id,
                        ProgramIcon = iconType,
                        AbsoluteUrl = hit.Source.OriginalFilePath,
                        DocumentType = hit.Source.ContentType
                    };
                });

                outResponse.Title = $"Doc.Search - Ihre Suche nach {searchPhrase}";

                var searchResults = await _viewToStringRenderer.Render("SearchResultsPartial", retCol);
                outResponse.SearchResults = searchResults;
                return outResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                return new DoSearchResponse();
            }
        }
    }
}