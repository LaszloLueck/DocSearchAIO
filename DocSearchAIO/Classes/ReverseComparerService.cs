using System.Collections.Concurrent;
using System.Diagnostics;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt;

namespace DocSearchAIO.Classes;

internal static class ReverseComparerServiceHelper
{
    public static Source<Option<ComparerObject>, NotUsed> CheckCacheEntry(
        this Source<ComparerObject, NotUsed> source,
        InterlockedCounter allFileCount)
    {
        return source.Select(compObj =>
        {
            allFileCount.Increment();
            return File.Exists(compObj.OriginalPath)
                ? Option<ComparerObject>.None
                : compObj;
        });
    }

    public static Source<IEnumerable<bool>, NotUsed> RemoveFromCache(
        this Source<IEnumerable<ComparerObject>, NotUsed> source,
        ConcurrentDictionary<string, ComparerObject> memoryCache) =>
        source.Select(group => group.Select(cmpObject => memoryCache.TryRemove(cmpObject.PathHash, out _)));


    public static Source<IEnumerable<ComparerObject>, NotUsed> RemoveFromIndexById(
        this Source<IEnumerable<ComparerObject>, NotUsed> source, InterlockedCounter removedFileCount,
        IElasticSearchService elasticSearchService, ILogger logger, string indexName)
    {
        return source.SelectAsyncUnordered(10, async items =>
        {
            var itemsArray = items.ToArray();

            if (itemsArray.Length <= 0) return Array.Empty<ComparerObject>();
            logger.LogInformation("try to remove {Elements} from elastic index", itemsArray.Length);
            var resultCount =
                await elasticSearchService.RemoveItemsById(indexName, itemsArray.Map(item => item.PathHash));
            removedFileCount.Add(resultCount);
            return itemsArray.AsEnumerable();
        });
    }
}

public class ReverseComparerService<T> where T : ComparerModel
{
    private readonly ILogger _logger;
    private readonly string _comparerFile;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ActorSystem _actorSystem;
    private readonly InterlockedCounter _allFileCount;
    private readonly InterlockedCounter _removedFileCount;
    private readonly ConcurrentDictionary<string, ComparerObject> _lazyCache;

    public ReverseComparerService(ILoggerFactory loggerFactory, T model, IElasticSearchService elasticSearchService,
        ActorSystem actorSystem)
    {
        _logger = loggerFactory.CreateLogger<ReverseComparerService<T>>();
        _comparerFile = model.ComparerFilePath;
        _elasticSearchService = elasticSearchService;
        _actorSystem = actorSystem;
        _allFileCount = new InterlockedCounter();
        _removedFileCount = new InterlockedCounter();
        _lazyCache = new ConcurrentDictionary<string, ComparerObject>();
        ComparerHelper.FillConcurrentDictionary(_comparerFile, _logger);
    }

    public async Task Process(string indexName)
    {
        _logger.LogInformation("process comparer file {ComparerFile}", _comparerFile);
        _logger.LogInformation("process elastic index {IndexName}", indexName);
        var sw = Stopwatch.StartNew();
        try
        {
            await ReverseComparerServiceHelper.RemoveFromIndexById(ComparerHelper
                    .GetComparerObjectSource(_comparerFile)
                    .CheckCacheEntry(_allFileCount)
                    .GroupedWithin(200, TimeSpan.FromSeconds(2))
                    .WithMaybeFilter(), _removedFileCount, _elasticSearchService, _logger, indexName)
                .RemoveFromCache(_lazyCache)
                .RunIgnore(_actorSystem.Materializer());


            if (_removedFileCount.Current() > 0)
            {
                _logger.LogInformation("switch comparer file, there are elements removed");
                _logger.LogInformation("try to remove comparer file {ComparerFile}", _comparerFile);
                ComparerHelper.RemoveComparerFile(_comparerFile);
                _logger.LogInformation("create new comparer file {ComparerFile}", _comparerFile);
                ComparerHelper.CreateComparerFile(_comparerFile);
                _logger.LogInformation("write all new entries from cache to comparer file {ComparerFile}",
                    _comparerFile);
                _logger.LogInformation("cache have {CacheSize} entries", _lazyCache.Count);
                await ComparerHelper.WriteAllLinesAsync(_lazyCache, _comparerFile);
            }
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "cleanup done {ElapsedTime} ms, process {AllCount} files, remove {RemovedCount} entries",
                sw.ElapsedMilliseconds, _allFileCount.Current(), _removedFileCount.Current());
            _logger.LogInformation("finished processing cleanup job");
        }
    }
}