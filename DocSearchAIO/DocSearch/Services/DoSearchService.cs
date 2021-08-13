using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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


        public async Task<DoSearchResponse> DoSearch(DoSearchRequest doSearchRequest)
        {
            try
            {
                var from = doSearchRequest.From;
                var size = doSearchRequest.Size;
                var searchPhrase = doSearchRequest.SearchPhrase == "" ? "*" : doSearchRequest.SearchPhrase;
                var query = new SimpleQueryStringQuery();
                var include = new[] { new Field("comments.comment"), new Field("content") };
                query.Fields = include;
                query.Query = searchPhrase;
                query.AnalyzeWildcard = true;

                var highlight = new Highlight
                {
                    PreTags = new[] { "<span style=\"color:orange;\">" },
                    PostTags = new[] { "</span>" },
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


                var selectedIndices = new List<string>();
                var enumerable = knownIndices.ResolveNullable(Array.Empty<string>(), (v, _) => v.ToArray());
                if (StaticHelpers.GetIndexKeyExpressionFromConfiguration(_configurationObject, enumerable, nameof(WordElasticDocument)) &&
                    doSearchRequest.FilterWord)
                    selectedIndices.Add($"{_configurationObject.IndexName}-{_configurationObject.Processing[nameof(WordElasticDocument)].IndexSuffix}");
                if (StaticHelpers.GetIndexKeyExpressionFromConfiguration(_configurationObject, enumerable, nameof(ExcelElasticDocument)) &&
                    doSearchRequest.FilterExcel)
                    selectedIndices.Add($"{_configurationObject.IndexName}-{_configurationObject.Processing[nameof(ExcelElasticDocument)].IndexSuffix}");
                if (StaticHelpers.GetIndexKeyExpressionFromConfiguration(_configurationObject, enumerable, nameof(PowerpointElasticDocument)) &&
                    doSearchRequest.FilterPowerpoint)
                    selectedIndices.Add($"{_configurationObject.IndexName}-{_configurationObject.Processing[nameof(PowerpointElasticDocument)].IndexSuffix}");
                if (StaticHelpers.GetIndexKeyExpressionFromConfiguration(_configurationObject, enumerable, nameof(PdfElasticDocument)) &&
                    doSearchRequest.FilterPdf)
                    selectedIndices.Add($"{_configurationObject.IndexName}-{_configurationObject.Processing[nameof(PdfElasticDocument)].IndexSuffix}");

                if (selectedIndices.Count == 4)
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
                        new ContentTypeAndValues(o.Key, o.Select(s => s.Item2)));

                    DoSearchResultContainer container = hit.Source;
                    container.SearchBody = grouped;
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
    }
}