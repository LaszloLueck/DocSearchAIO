using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.Extensions.Logging;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services
{
    public class SearchSuggestService
    {
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ILogger<SearchSuggestService> _logger;

        public SearchSuggestService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SearchSuggestService>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task<SuggestResult> GetSuggestions(string searchPhrase)
        {
            var suggestQuery = new SuggestContainer();
            var suggestBucket = new SuggestBucket();
            var completionSuggester = new CompletionSuggester();
            suggestBucket.Prefix = searchPhrase;
            completionSuggester.Analyzer = "simple";
            completionSuggester.Size = 10;
            completionSuggester.Field = new Field("completionContent");
            completionSuggester.SkipDuplicates = true;
            suggestBucket.Completion = completionSuggester;

            suggestQuery.Add("searchfieldsuggest", suggestBucket);


            _logger.LogInformation($"Build SuggestQuery. Raw query is:");

            var f = new SourceFilter {Excludes = "*"};

            var request = new SearchRequest("officedocuments-*") {Suggest = suggestQuery, Source = f};
            var sw = Stopwatch.StartNew();
            var result = await _elasticSearchService.SearchIndexAsync<WordElasticDocument>(request);
            sw.Stop();
            var suggestsEntries = result.Suggest["searchfieldsuggest"];
            var suggestResult = suggestsEntries.First();
            _logger.LogInformation($"found {suggestResult.Options.Count} suggests in {sw.ElapsedMilliseconds} ms");
            var suggests = suggestResult.Options.Select(d => new SuggestEntry {id = d.Id, label = d.Text});

            return new SuggestResult
            {
                SearchPhrase = searchPhrase,
                suggests = suggests
            };
        }
    }
}