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
                .AsGenericSourceString()
                .DirectoryNotExistsAction(source =>
                { 
                    Directory.CreateDirectory(source.Value);
                    return source;
                })
                .AndThen(source =>
                {
                    ILiteDatabase db = new LiteDatabase($"{source.Value}/docsearchaio.db");
                    services.AddSingleton(db); 
                });
        }
    }
}