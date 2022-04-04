using System.Diagnostics;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services;

public class DocumentDetailService
{
    private readonly ILogger _logger;
    private readonly IElasticSearchService _elasticSearchService;

    public DocumentDetailService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DocumentDetailService>();
        _elasticSearchService = elasticSearchService;
    }

    public async Task<DocumentDetailModel> DocumentDetail(
        DocumentDetailRequest request)
    {
        try
        {
            var query = new TermQuery {Field = new Field("_id"), Value = request.Id};
            var exclude = new[] {new Field("completionContent"), new Field("content")};
            var sf = new SourceFilter {Excludes = exclude};
            var searchRequest = new SearchRequest
            {
                Query = new QueryContainer(query), Source = sf
            };
            var sw = Stopwatch.StartNew();
            var result = await _elasticSearchService.SearchIndexAsync<WordElasticDocument>(searchRequest);
            sw.Stop();
            _logger.LogInformation("find document detail in {ElapsedTimeMs} ms", sw.ElapsedMilliseconds);
            return result
                .Hits
                .TryFirst()
                .Match(
                    hit =>
                    {
                        DocumentDetailModel source = hit.Source;
                        source.Id = hit.Id;
                        return source;
                    },
                    () => new DocumentDetailModel(string.Empty, new DateTime(1970,01,01,0,0,0), string.Empty, new DateTime(1970,01,01,0,0,0),
                        string.Empty, string.Empty, string.Empty, string.Empty, new DateTime(1970,01,01,0,0,0), string.Empty, new DateTime(1970,01,01,0,0,0))
                );
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occured");
            return new DocumentDetailModel(string.Empty, new DateTime(1970,01,01,0,0,0), string.Empty, new DateTime(1970,01,01,0,0,0),
                string.Empty, string.Empty, string.Empty, string.Empty, new DateTime(1970,01,01,0,0,0), string.Empty, new DateTime(1970,01,01,0,0,0));
        }
    }
}