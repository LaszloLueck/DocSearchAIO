using System.Diagnostics;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Endpoints.Suggest;
using DocSearchAIO.Services;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services;

public interface ISearchSuggestService
{
    public Task<SuggestResult> Suggestions(string searchPhrase);
}

public class SearchSuggestService : ISearchSuggestService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ILogger<SearchSuggestService> _logger;
    private readonly ConfigurationObject _configurationObject;

    public SearchSuggestService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SearchSuggestService>();
        _elasticSearchService = elasticSearchService;
        var cfgTmp = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(cfgTmp);
        _configurationObject = cfgTmp;
    }

    public async Task<SuggestResult> Suggestions(string searchPhrase)
    {
        var suggestQuery = new SuggestContainer();
        var suggestBucket = new SuggestBucket();
        var completionSuggester = new CompletionSuggester();
        suggestBucket.Prefix = searchPhrase;
        completionSuggester.Analyzer = "keyword";
        completionSuggester.Size = 10;
        completionSuggester.Field = new Field("completionContent");
        completionSuggester.SkipDuplicates = true;
        suggestBucket.Completion = completionSuggester;

        suggestQuery.Add("searchfieldsuggest", suggestBucket);


        _logger.LogInformation("Build SuggestQuery. Raw query is:");

        var f = new SourceFilter {Excludes = "*"};

        var request = new SearchRequest($"{_configurationObject.IndexName}-*") {Suggest = suggestQuery, Source = f};
        var sw = Stopwatch.StartNew();
        var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
        sw.Stop();
        var suggestsEntries = result.Suggest["searchfieldsuggest"];
        var suggestResult = suggestsEntries.First();
        _logger.LogInformation("found {SuggestResultCount} suggests in {ElapsedTimeMs} ms",
            suggestResult.Options.Count, sw.ElapsedMilliseconds);
        var suggests = suggestResult.Options.Map(d => new SuggestEntry(d.Id, d.Text));

        return new SuggestResult(searchPhrase, suggests);
    }
}