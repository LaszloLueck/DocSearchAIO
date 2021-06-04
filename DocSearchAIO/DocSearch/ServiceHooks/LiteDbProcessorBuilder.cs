using System.IO;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class LiteDbProcessorBuilder
    {
        public static void AddLiteDb(this IServiceCollection services, IConfiguration configuration)
        {
            if (!Directory.Exists("./litedb"))
            {
                Directory.CreateDirectory("./litedb");
            }
            var db = new LiteDatabase(@"./litedb/docsearchaio.db");
            services.AddSingleton<LiteDatabase>(db);
        }
    }
}