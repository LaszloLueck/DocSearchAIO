using DocSearchAIO.Classes;
using DocSearchAIO.Services;

namespace DocSearchAIO.Utilities
{
    public class ElasticUtilities
    {
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ILogger _logger;

        public ElasticUtilities(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<ElasticUtilities>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument
        {
            if (!await _elasticSearchService.IndexExistsAsync(indexName))
            {
                _logger.LogInformation("Index {IndexName} does not exist, lets create them", indexName);
                await _elasticSearchService.CreateIndexAsync<T>(indexName);
                await _elasticSearchService.RefreshIndexAsync(indexName);
                await _elasticSearchService.FlushIndexAsync(indexName);
            }
        }

        public readonly Func<string, string, string> CreateIndexName = (mainName, suffix) => $"{mainName}-{suffix}";
    }
}