using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;

namespace DocSearchAIO.Utilities;

public interface IElasticUtilities
{
    public Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument;
    public string CreateIndexName(string mainName, string suffix);
}

public class ElasticUtilities : IElasticUtilities
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ILogger _logger;

    public ElasticUtilities(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
    {
        _logger = LoggingFactoryBuilder.Build<ElasticUtilities>();
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

    public string CreateIndexName(string mainName, string suffix) => _createIndexNameImpl(mainName, suffix);

    private readonly Func<string, string, string> _createIndexNameImpl = (mainName, suffix) => $"{mainName}-{suffix}";
}