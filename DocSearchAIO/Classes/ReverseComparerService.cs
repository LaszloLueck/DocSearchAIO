using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Common.Logging;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    internal static class ReverseComparerServiceHelper
    {
        public static Source<Maybe<ComparerObject>, NotUsed> CheckCacheEntry(
            this Source<ComparerObject, NotUsed> source,
            InterlockedCounter allFileCount)
        {
            return source.Select(compObj =>
            {
                allFileCount.Increment();
                return File.Exists(compObj.OriginalPath)
                    ? Maybe<ComparerObject>.None
                    : Maybe<ComparerObject>.From(compObj);
            });

            //if (File.Exists(compObj.OriginalPath)) return false;
            //removedFileCount.Increment();
            //logger.LogInformation("remove obsolete file <{ObsoleteFile}>", compObj.OriginalPath);
            //cache.TryRemove(new KeyValuePair<string, ComparerObject>(compObj.PathHash, compObj));
            //return await elasticSearchService.RemoveItemById(indexName, compObj.PathHash);
        }

        public static Source<IEnumerable<ComparerObject>, NotUsed> RemoveFromIndexById(
            this Source<IEnumerable<ComparerObject>, NotUsed> source, InterlockedCounter removedFileCount,
            IElasticSearchService elasticSearchService, ILogger logger, string indexName)
        {
            return source.SelectAsyncUnordered(10, async items =>
            {
                var asArray = items.ToArray();
                var resultCount =
                    await elasticSearchService.RemoveItemsById(indexName, asArray.Select(item => item.PathHash));
                removedFileCount.Add(resultCount);
                return asArray.Select(p => p);
            });
        }
    }


    //, InterlockedCounter removedFileCount, ILogger logger, IElasticSearchService elasticSearchService,ConcurrentDictionary<string, ComparerObject> cache, string indexName

    public class ReverseComparerService<T> where T : ComparerModel
    {
        private readonly ILogger _logger;
        private readonly string _comparerFile;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ActorSystem _actorSystem;
        private readonly InterlockedCounter _allFileCount;
        private readonly InterlockedCounter _removedFileCount;
        private Lazy<ConcurrentDictionary<string, ComparerObject>> _lazyCache;

        public ReverseComparerService(ILoggerFactory loggerFactory, T model, IElasticSearchService elasticSearchService,
            ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<ReverseComparerService<T>>();
            _comparerFile = model.GetComparerFilePath;
            _elasticSearchService = elasticSearchService;
            _actorSystem = actorSystem;
            _allFileCount = new InterlockedCounter();
            _removedFileCount = new InterlockedCounter();
            _lazyCache =
                new Lazy<ConcurrentDictionary<string, ComparerObject>>(() =>
                    ComparerHelper.FillConcurrentDictionary(_comparerFile));
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
                    
                    //var cache = ComparerHelper.FillConcurrentDictionary(_comparerFile);
                    // await ComparerHelper
                    //         .GetComparerObjectSource(_comparerFile)
                    //         .SelectAsyncUnordered(10, async compObj =>
                    //             await compObj
                    //                 .CheckCacheEntry(_allFileCount))
                    //         .GroupedWithin(1000, TimeSpan.FromSeconds(10))
                    //         .Select(group => group.Values())
                    //         .Select(values => { });
                    //     .RunWith(Sink.Ignore<bool>(), _actorSystem.Materializer());
                    // if (_removedFileCount.GetCurrent() > 0)
                    // {
                    //     _logger.LogInformation("switch comparer file, there are elements removed");
                    //     ComparerHelper.RemoveComparerFile(_comparerFile);
                    //     ComparerHelper.CreateComparerFile(_comparerFile);
                    //     await ComparerHelper.WriteAllLinesAsync(cache, _comparerFile);
                    // }
                    // else
                    // {
                    //     _logger.LogInformation("nothing to do, no elements found which are removed while cleanup");
                    // }
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