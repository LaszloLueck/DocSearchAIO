using DocSearchAIO.Services;
using Elasticsearch.Net;
using Nest;

namespace DocSearchAIO.DocSearch.ServiceHooks;

public interface IElasticSearchExtensions
{
    public IElasticSearchService AddElasticSearch();
}


public class ElasticSearchExtensions : IElasticSearchExtensions
{
    private readonly IConfigurationUpdater _configurationUpdater;

    public ElasticSearchExtensions(IConfigurationUpdater configurationUpdater)
    {
        _configurationUpdater = configurationUpdater;
    }
    
    public IElasticSearchService AddElasticSearch()
    {
        var cfg = _configurationUpdater.ReadConfiguration();
        var uriList = cfg.ElasticEndpoints.Map(e => new Uri(e));
        var pool = new StaticConnectionPool(uriList);
        var settings = new ConnectionSettings(pool)
            .DefaultIndex(cfg.IndexName)
            .BasicAuthentication(cfg.ElasticUser, cfg.ElasticPassword)
            .PrettyJson();
        var client = new ElasticClient(settings);
        return new ElasticSearchService(client);
    }
}