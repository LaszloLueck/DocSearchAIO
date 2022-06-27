using System.Diagnostics;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Endpoints.Suggest;
using DocSearchAIO.Services;
using LanguageExt;
using LanguageExt.SomeHelp;
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

        var f = new SourceFilter {Excludes = "*"};
        

        static Option<IndexName> CheckIndexName(string indexName, bool toCheck)
        {
            return toCheck ? toIndexName(indexName) : Option<IndexName>.None;
        }

        static IndexName toIndexName(string indexName) => indexName;

        var indices = Seq(
            CheckIndexName($"{_configurationObject.IndexName}-word", suggestRequest.SuggestWord),
            CheckIndexName($"{_configurationObject.IndexName}-excel", suggestRequest.SuggestExcel),
            CheckIndexName($"{_configurationObject.IndexName}-powerpoint", suggestRequest.SuggestPowerpoint),
            CheckIndexName($"{_configurationObject.IndexName}-pdf", suggestRequest.SuggestPdf),
            CheckIndexName($"{_configurationObject.IndexName}-eml", suggestRequest.SuggestEml),
            CheckIndexName($"{_configurationObject.IndexName}-msg", suggestRequest.SuggestMsg)
        ).Somes();


        var sw = Stopwatch.StartNew();
        var resultsAsync = indices.Map(async index =>
        {
            var request = new SearchRequest(index) {Suggest = suggestQuery, Source = f};
            var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
            var entries = result.Suggest["searchfieldsuggest"].First();
            return entries.Options.Map(entry => (entry.Text, entry.Index.Name));
        });

        var results = await Task.WhenAll(resultsAsync);

        var grouped = results
            .Flatten()
            .GroupBy(d => d.Text)
            .Map(m => (m.Key, m.Select(d => d.Name)))
            .Map(t => new SuggestEntry(t.Key, t.Item2.ToArray()))
            .Take(10)
            .OrderBy(d => d.IndexName.Length)
            .Rev();
        
        

        //var request = new SearchRequest($"{_configurationObject.IndexName}-*") {Suggest = suggestQuery, Source = f};


        sw.Stop();
        //var suggestsEntries = result.Suggest["searchfieldsuggest"];
        //var suggestResult = suggestsEntries.First();
        //_logger.LogInformation("found {SuggestResultCount} suggests in {ElapsedTimeMs} ms",
        //    suggestResult.Options.Count, sw.ElapsedMilliseconds);
        //var suggests = suggestResult.Options.Map(d => new SuggestEntry(d.Id, d.Text, d.Index.Name));

        return new SuggestResult(suggestRequest.SearchPhrase, grouped);
    }
}