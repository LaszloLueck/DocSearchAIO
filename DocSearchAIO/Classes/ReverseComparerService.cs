using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Services;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public class ReverseComparerService<T> where T : ComparerModel
    {
        private readonly ILogger _logger;
        private readonly string _comparerFile;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ActorSystem _actorSystem;

        public ReverseComparerService(ILoggerFactory loggerFactory, T model, IElasticSearchService elasticSearchService, ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<ReverseComparerService<T>>();
            _comparerFile = model.GetComparerFilePath;
            _elasticSearchService = elasticSearchService;
            _actorSystem = actorSystem;
        }

        public async Task Process(string indexName)
        {
            await Task.Run(async () =>
            {
                _logger.LogInformation("process comparer file {ComparerFile}", _comparerFile);
                _logger.LogInformation("process elastic index {IndexName}", indexName);

                var cache = ComparerHelper.FillConcurrentDictionary(_comparerFile);
                await ComparerHelper
                    .GetComparerObjectSource(_comparerFile)
                    .Select(async compObj =>
                    {
                        if (!File.Exists(compObj.OriginalPath))
                        {
                            _logger.LogInformation("remove obsolete file <{ObsoleteFile}>", compObj.OriginalPath);
                            cache.Remove(compObj.OriginalPath, out var nCompObj);
                            await _elasticSearchService.RemoveItemById(indexName, compObj.PathHash);
                        }
                    })
                    .RunWith(Sink.Ignore<Task>(), _actorSystem.Materializer());
                
                
                
            });
        }
    }
}