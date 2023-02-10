using System.Text.Json.Serialization;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.Scheduler;

public static class JobStateMemoryCacheProxy
{
    public static readonly Func<IMemoryCache,
            Seq<(IProcessorBase, Func<MemoryCacheModel>)>>
        AsIEnumerable = memoryCache =>
        {
            return Seq<(IProcessorBase, Func<MemoryCacheModel>)>(
                (new ProcessorBaseWord(),
                    () => new MemoryCacheModelWord(memoryCache)),
                (new ProcessorBasePowerpoint(),
                    () => new MemoryCacheModelPowerpoint(memoryCache)),
                (new ProcessorBasePdf(),
                    () => new MemoryCacheModelPdf(memoryCache)),
                (new ProcessorBaseExcel(),
                    () => new MemoryCacheModelExcel(memoryCache)),
                (new ProcessorBaseMsg(),
                    () => new MemoryCacheModelMsg(memoryCache)),
                (new ProcessorBaseEml(),
                    () => new MemoryCacheModelEml(memoryCache))
            );
        };

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelWord>>
        GetWordJobStateMemoryCache =
            memoryCache =>
                new JobStateMemoryCache<MemoryCacheModelWord>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelPowerpoint>>
        GetPowerpointJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelPowerpoint>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelPdf>>
        GetPdfJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelPdf>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelExcel>>
        GetExcelJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelExcel>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelMsg>>
        GetMsgJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelMsg>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelEml>>
        GetEmlJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelEml>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelExcelCleanup>>
        GetExcelCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelExcelCleanup>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelPdfCleanup>>
        GetPdfCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelPdfCleanup>(memoryCache);

    public static readonly
        Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelPowerpointCleanup>>
        GetPowerpointCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelPowerpointCleanup>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelWordCleanup>>
        GetWordCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelWordCleanup>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelMsgCleanup>>
        GetMsgCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelMsgCleanup>(memoryCache);

    public static readonly Func<IMemoryCache, JobStateMemoryCache<MemoryCacheModelEmlCleanup>>
        GetEmlCleanupJobStateMemoryCache = memoryCache =>
            new JobStateMemoryCache<MemoryCacheModelEmlCleanup>(memoryCache);
}

public class JobStateMemoryCache<TModel> where TModel : MemoryCacheModel
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public JobStateMemoryCache(IMemoryCache memoryCache)
    {
        _logger = LoggingFactoryBuilder.Build<JobStateMemoryCache<TModel>>();
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
        _logger.LogInformation("remove cache entry for {Name}", typeof(TModel).Name);
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
    [JsonPropertyName("cacheKey")] public string CacheKey { get; set; } = string.Empty;
    [JsonPropertyName("dateTime")] public DateTime DateTime { get; set; }
    [JsonPropertyName("jobState")] public JobState JobState { get; set; }
}