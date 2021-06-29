using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Scheduler;
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
        private readonly InterlockedCounter _allFileCount;
        private readonly InterlockedCounter _removedFileCount;

        public ReverseComparerService(ILoggerFactory loggerFactory, T model, IElasticSearchService elasticSearchService,
            ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<ReverseComparerService<T>>();
            _comparerFile = model.GetComparerFilePath;
            _elasticSearchService = elasticSearchService;
            _actorSystem = actorSystem;
            _allFileCount = new InterlockedCounter();
            _removedFileCount = new InterlockedCounter();
        }

        public async Task Process(string indexName)
        {
            await Task.Run(async () =>
            {
                _logger.LogInformation("process comparer file {ComparerFile}", _comparerFile);
                _logger.LogInformation("process elastic index {IndexName}", indexName);
                var sw = Stopwatch.StartNew();
                try
                {
                    var cache = ComparerHelper.FillConcurrentDictionary(_comparerFile);
                    await ComparerHelper
                        .GetComparerObjectSource(_comparerFile)
                        .SelectAsyncUnordered(10, async compObj =>
                        {
                            return await Task.Run(async () =>
                            {
                                _allFileCount.Increment();
                                if (File.Exists(compObj.OriginalPath)) return false;
                                _removedFileCount.Increment();
                                _logger.LogInformation("remove obsolete file <{ObsoleteFile}>", compObj.OriginalPath);
                                cache.TryRemove(new KeyValuePair<string, ComparerObject>(compObj.PathHash, compObj));
                                return await _elasticSearchService.RemoveItemById(indexName, compObj.PathHash);

                            });
                        })
                        .RunWith(Sink.Ignore<bool>(), _actorSystem.Materializer());
                    if (_removedFileCount.GetCurrent() > 0)
                    {
                        _logger.LogInformation("switch comparer file, there are elements removed");
                        ComparerHelper.RemoveComparerFile(_comparerFile);
                        await ComparerHelper.WriteAllLinesAsync(cache, _comparerFile);
                    }
                    else
                    {
                        _logger.LogInformation("nothing to do, no elements found which are removed while cleanup");
                    }
                }
                finally
                {
                    sw.Stop();
                    _logger.LogInformation(
                        "cleanup done {ElapsedTime} ms, process {AllCount} files, remove {RemovedCount} entries",
                        sw.ElapsedMilliseconds, _allFileCount.GetCurrent(), _removedFileCount.GetCurrent());
                    _logger.LogInformation("finished processing cleanup job");
                }
            });
        }
    }
}