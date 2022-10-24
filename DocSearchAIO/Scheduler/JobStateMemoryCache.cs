using System.Text.Json.Serialization;
using DocSearchAIO.Classes;
using LanguageExt;
using LanguageExt.Pretty;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.Scheduler;

public static class JobStateMemoryCacheProxy
{
    public static readonly Func<ILoggerFactory, IMemoryCache,
            Seq<(ProcessorBase, Func<MemoryCacheModel>)>>
        AsIEnumerable = (loggerFactory, memoryCache) =>
        {
            return Seq<(ProcessorBase, Func<MemoryCacheModel>)>(
                (new ProcessorBaseWord(),
                    () => new MemoryCacheModelWord(loggerFactory, memoryCache)),
                (new ProcessorBasePowerpoint(),
                    () => new MemoryCacheModelPowerpoint(loggerFactory, memoryCache)),
                (new ProcessorBasePdf(),
                    () => new MemoryCacheModelPdf(loggerFactory, memoryCache)),
                (new ProcessorBaseExcel(),
                    () => new MemoryCacheModelExcel(loggerFactory, memoryCache)),
                (new ProcessorBaseMsg(),
                    () => new MemoryCacheModelMsg(loggerFactory, memoryCache)),
                (new ProcessorBaseEml(),
                    () => new MemoryCacheModelEml(loggerFactory, memoryCache))
            );
        };

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelWord>>
        GetWordJobStateMemoryCache =
            (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<MemoryCacheModelWord>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPowerpoint>>
        GetPowerpointJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelPowerpoint>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPdf>>
        GetPdfJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelPdf>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelExcel>>
        GetExcelJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelExcel>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelMsg>>
        GetMsgJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelMsg>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelEml>>
        GetEmlJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelEml>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelExcelCleanup>>
        GetExcelCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelExcelCleanup>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPdfCleanup>>
        GetPdfCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelPdfCleanup>(loggerFactory, memoryCache);

    public static readonly
        Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPowerpointCleanup>>
        GetPowerpointCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelPowerpointCleanup>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelWordCleanup>>
        GetWordCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelWordCleanup>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelMsgCleanup>>
        GetMsgCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelMsgCleanup>(loggerFactory, memoryCache);

    public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelEmlCleanup>>
        GetEmlCleanupJobStateMemoryCache = (loggerFactory, memoryCache) =>
            new JobStateMemoryCache<MemoryCacheModelEmlCleanup>(loggerFactory, memoryCache);
}

public class JobStateMemoryCache<TModel> where TModel : MemoryCacheModel
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public JobStateMemoryCache(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
    {
        _logger = loggerFactory.CreateLogger<JobStateMemoryCache<TModel>>();
        _memoryCache = memoryCache;
    }


    public Option<CacheEntry> CacheEntry<TCacheModel>(TCacheModel model) where TCacheModel : MemoryCacheModel
    {
        _logger.LogInformation("try to get cache entry for model {ModelName}", model.GetType().Name);
        return _memoryCache.TryGetValue(model, out CacheEntry? cacheEntry)
            ? cacheEntry
            : Option<CacheEntry>.None;
    }


    public void RemoveCacheEntry()
    {
        _logger.LogInformation("remove cache entry");
        _memoryCache.Remove(typeof(TModel).Name);
    }

    public void SetCacheEntry(JobState jobState)
    {
        _logger.LogInformation("set cache entry to {JobState}", jobState);
        var cacheEntry = new CacheEntry {CacheKey = typeof(TModel).Name, DateTime = DateTime.Now, JobState = jobState};
        _memoryCache.Set(cacheEntry.CacheKey, cacheEntry);
    }
}

public enum JobState
{
    Running = 0,
    Stopped = 1,
    Undefined = 2
}

public class CacheEntry
{
    [JsonPropertyName("cacheKey")]
    public string CacheKey { get; set; } = string.Empty;
    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }
    [JsonPropertyName("jobState")]
    public JobState JobState { get; set; }
}