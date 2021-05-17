using System;
using System.Collections.Generic;
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
            var uriList = new List<Uri> {new("http://127.0.0.1:9200")};
            var pool = new StaticConnectionPool(uriList);
            var defaultIndex = "officedocuments";
            var settings = new ConnectionSettings(pool).DefaultIndex(defaultIndex);
            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);

        }
    }
}