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
        private readonly ElasticSearchService _elasticSearchService;
        private readonly ILogger<SearchSuggestService> _logger;

        public SearchSuggestService(IElasticClient elasticClient, ILoggerFactory loggerFactory)
        {
            _elasticSearchService = new ElasticSearchService(loggerFactory, elasticClient);
            _logger = loggerFactory.CreateLogger<SearchSuggestService>();
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

            // var i1 = "*";
            // var i = Indices.Index()

            var request = new SearchRequest("officedocuments-*") {Suggest = suggestQuery, Source = f};
            var sw = Stopwatch.StartNew();
            var result = await _elasticSearchService.SearchIndexAsync<ElasticDocument>(request);
            sw.Stop();
            var suggestResult = result.Suggest["searchfieldsuggest"];
            var suggest = suggestResult.First();
            var suggests = suggest.Options.Select(d => new SuggestEntry() {id = d.Id, label = d.Text});

            return new SuggestResult()
            {
                DocCount = result.Total, SearchPhrase = searchPhrase, SearchTime = sw.ElapsedMilliseconds,
                suggests = suggests
            };
        }
    }
}