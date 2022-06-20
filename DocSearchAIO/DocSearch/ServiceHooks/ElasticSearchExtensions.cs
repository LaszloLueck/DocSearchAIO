using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using Elasticsearch.Net;
using Nest;

namespace DocSearchAIO.DocSearch.ServiceHooks;

public static class ElasticSearchExtensions
{
    public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
    {
        var cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(cfg);
        var uriList = cfg.ElasticEndpoints.Map(e => new Uri(e));
        var pool = new StaticConnectionPool(uriList);
        var settings = new ConnectionSettings(pool)
            .DefaultIndex(cfg.IndexName)
            .BasicAuthentication(cfg.ElasticUser, cfg.ElasticPassword)
            .PrettyJson();
            var client = new ElasticClient(settings);
        var elasticSearchService = new ElasticSearchService(client);
        services.AddSingleton<IElasticSearchService>(elasticSearchService);
    }
}