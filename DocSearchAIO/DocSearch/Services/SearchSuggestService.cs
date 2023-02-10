using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Endpoints.Suggest;
using DocSearchAIO.Services;
using LanguageExt;
using MethodTimer;
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
    private readonly IConfigurationUpdater _configurationUpdater;

    public SearchSuggestService(IElasticSearchService elasticSearchService,
        IConfigurationUpdater configurationUpdater)
    {
        _logger = LoggingFactoryBuilder.Build<SearchSuggestService>();
        _elasticSearchService = elasticSearchService;
        _configurationUpdater = configurationUpdater;
    }


    [Time]
    public async Task<SuggestResult> Suggestions(SuggestRequest suggestRequest)
    {
        var suggestQuery = new SuggestContainer();
        var suggestBucket = new SuggestBucket();
        var completionSuggester = new CompletionSuggester();
        var configurationObject = await _configurationUpdater.ReadConfigurationAsync();
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
            CheckIndexName($"{configurationObject.IndexName}-word", suggestRequest.SuggestWord),
            CheckIndexName($"{configurationObject.IndexName}-excel", suggestRequest.SuggestExcel),
            CheckIndexName($"{configurationObject.IndexName}-powerpoint", suggestRequest.SuggestPowerpoint),
            CheckIndexName($"{configurationObject.IndexName}-pdf", suggestRequest.SuggestPdf),
            CheckIndexName($"{configurationObject.IndexName}-eml", suggestRequest.SuggestEml),
            CheckIndexName($"{configurationObject.IndexName}-msg", suggestRequest.SuggestMsg)
        );

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
        return new SuggestResult(suggestRequest.SearchPhrase, grouped);
    }
}

public static class SearchSuggestHelper
{
    public static async Task<IEnumerable<T>> ResolveHelper<T>(this IEnumerable<Task<T>> source, int parallelism) =>
        await source.SequenceParallel(parallelism);
}