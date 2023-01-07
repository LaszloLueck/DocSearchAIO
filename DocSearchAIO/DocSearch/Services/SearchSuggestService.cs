using System.Diagnostics;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Endpoints.Suggest;
using DocSearchAIO.Services;
using LanguageExt;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services;

public interface ISearchSuggestService
{
    public Task<SuggestResult> Suggestions(SuggestRequest suggestRequest);
}

public class SearchSuggestService : ISearchSuggestService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ILogger<SearchSuggestService> _logger;
    private readonly ConfigurationObject _configurationObject;

    public SearchSuggestService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SearchSuggestService>();
        _elasticSearchService = elasticSearchService;
        var cfgTmp = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(cfgTmp);
        _configurationObject = cfgTmp;
    }

    public async Task<SuggestResult> Suggestions(SuggestRequest suggestRequest)
    {
        var suggestQuery = new SuggestContainer();
        var suggestBucket = new SuggestBucket();
        var completionSuggester = new CompletionSuggester();
        suggestBucket.Prefix = suggestRequest.SearchPhrase;
        completionSuggester.Analyzer = "keyword";
        completionSuggester.Size = 10;
        completionSuggester.Field = new Field("completionContent");
        completionSuggester.SkipDuplicates = true;
        suggestBucket.Completion = completionSuggester;

        suggestQuery.Add("searchfieldsuggest", suggestBucket);


        _logger.LogInformation("Build SuggestQuery. Raw query is:");

        var f = new SourceFilter { Excludes = "*" };


        static Option<IndexName> CheckIndexName(string indexName, bool toCheck)
        {
            static IndexName ToIndexName(string indexName) => indexName;
            return toCheck ? ToIndexName(indexName) : Option<IndexName>.None;
        }


        var indices = Seq(
            CheckIndexName($"{_configurationObject.IndexName}-word", suggestRequest.SuggestWord),
            CheckIndexName($"{_configurationObject.IndexName}-excel", suggestRequest.SuggestExcel),
            CheckIndexName($"{_configurationObject.IndexName}-powerpoint", suggestRequest.SuggestPowerpoint),
            CheckIndexName($"{_configurationObject.IndexName}-pdf", suggestRequest.SuggestPdf),
            CheckIndexName($"{_configurationObject.IndexName}-eml", suggestRequest.SuggestEml),
            CheckIndexName($"{_configurationObject.IndexName}-msg", suggestRequest.SuggestMsg)
        );


        var sw = Stopwatch.StartNew();
        var resultsAsync = await indices
            .Somes()
            .Map(async index =>
            {
                var request = new SearchRequest(index) { Suggest = suggestQuery, Source = f };
                var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
                var entries = result.Suggest["searchfieldsuggest"].First();
                return entries.Options.Map(entry => (SuggestEntry: entry.Text.ToString(), IndexName: entry.Index.Name.ToString()));
            })
            .ResolveHelper(2);

        var grouped = resultsAsync
            .Flatten()
            .GroupBy(d => d.SuggestEntry)
            .Map(m => (m.Key, m.Select(d => d.IndexName)))
            .Map(t => new SuggestEntry(t.Key, t.Item2.ToArray()))
            .Take(10);
        sw.Stop();
        return new SuggestResult(suggestRequest.SearchPhrase, grouped);
    }
}

public static class SearchSuggestHelper
{
    public static async Task<IEnumerable<T>> ResolveHelper<T>(this IEnumerable<Task<T>> source, int parallelism) =>
        await source.SequenceParallel(parallelism);
}