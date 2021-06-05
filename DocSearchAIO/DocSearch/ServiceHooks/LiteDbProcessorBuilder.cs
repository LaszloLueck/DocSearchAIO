using System.IO;
using DocSearchAIO.Scheduler;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class LiteDbProcessorBuilder
    {
        public static void AddLiteDb(this IServiceCollection services, IConfiguration configuration)
        {
            const string liteDbPath = "./litedb";
            
            liteDbPath
                .DirectoryNotExistsAction(p => Directory.CreateDirectory(p));

            ILiteDatabase db = new LiteDatabase($"{liteDbPath}/docsearchaio.db");
            services.AddSingleton(db);
        }
    }
}