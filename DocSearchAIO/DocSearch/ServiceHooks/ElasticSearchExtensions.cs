using System;
using System.Linq;
using DocSearchAIO.Configuration;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class ElasticSearchExtensions
    {
        public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfg);
            var uriList = cfg.ElasticEndpoints.Select(e => new Uri(e));
            var pool = new StaticConnectionPool(uriList);
            var settings = new ConnectionSettings(pool).DefaultIndex(cfg.IndexName);
            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);
        }
    }
}