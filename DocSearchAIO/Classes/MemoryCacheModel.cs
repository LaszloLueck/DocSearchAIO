using DocSearchAIO.Scheduler;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.Classes;

public class MemoryCacheModelProxy
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheModelProxy(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
    {
        _loggerFactory = loggerFactory;
        _memoryCache = memoryCache;
    }

    public Seq<(string, Func<MemoryCacheModel>)> Models()
    {
        return Seq<(string, Func<MemoryCacheModel>)>(
            ("wordProcessingJob", () => new MemoryCacheModelWord(_loggerFactory, _memoryCache)),
            ("excelProcessingJob", () => new MemoryCacheModelExcel(_loggerFactory, _memoryCache)),
            ("pdfProcessingJob", () => new MemoryCacheModelPdf(_loggerFactory, _memoryCache)),
            ("powerpointProcessingJob", () => new MemoryCacheModelPowerpoint(_loggerFactory, _memoryCache)),
            ("powerpointProcessingJob", () => new MemoryCacheModelPowerpoint(_loggerFactory, _memoryCache)),
            ("wordCleanupJob", () => new MemoryCacheModelWordCleanup(_loggerFactory, _memoryCache)),
            ("excelCleanupJob", () => new MemoryCacheModelExcelCleanup(_loggerFactory, _memoryCache)),
            ("powerpointCleanupJob", () => new MemoryCacheModelPowerpointCleanup(_loggerFactory, _memoryCache)),
            ("pdfCleanupJob", () => new MemoryCacheModelPdfCleanup(_loggerFactory, _memoryCache)));
    }
}

public abstract class MemoryCacheModel
{
    private readonly ILogger? _logger;
    private readonly IMemoryCache? _memoryCache;

    protected abstract string DerivedModelName { get; }

    protected MemoryCacheModel(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
    {
        _logger = loggerFactory.CreateLogger<MemoryCacheModel>();
        _memoryCache = memoryCache;
    }

    protected MemoryCacheModel()
    {
    }

    public Option<CacheEntry> CacheEntry()
    {
        _logger?.LogInformation("try to get cache entry for model {DerivedModelName}", DerivedModelName);
        _memoryCache.TryGetValue(DerivedModelName, out CacheEntry cacheEntry);
        return cacheEntry;
    }
}

public class MemoryCacheModelWord : MemoryCacheModel
{
    public MemoryCacheModelWord(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelWord()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelPowerpoint : MemoryCacheModel
{
    public MemoryCacheModelPowerpoint(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelPowerpoint()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelPdf : MemoryCacheModel
{
    public MemoryCacheModelPdf(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelPdf()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelExcel : MemoryCacheModel
{
    protected override string DerivedModelName => GetType().Name;

    public MemoryCacheModelExcel(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelExcel()
    {
    }
}

public class MemoryCacheModelExcelCleanup : MemoryCacheModel
{
    public MemoryCacheModelExcelCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(
        loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelExcelCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelMsg : MemoryCacheModel
{
    protected override string DerivedModelName => GetType().Name;

    public MemoryCacheModelMsg(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelMsg()
    {
    }
}

public class MemoryCacheModelMsgCleanup : MemoryCacheModel
{
    public MemoryCacheModelMsgCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelMsgCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelEml : MemoryCacheModel
{
    protected override string DerivedModelName => GetType().Name;

    public MemoryCacheModelEml(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelEml()
    {
    }
}

public class MemoryCacheModelEmlCleanup : MemoryCacheModel
{
    public MemoryCacheModelEmlCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelEmlCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelWordCleanup : MemoryCacheModel
{
    public MemoryCacheModelWordCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelWordCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelPowerpointCleanup : MemoryCacheModel
{
    public MemoryCacheModelPowerpointCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(
        loggerFactory, memoryCache)
    {
    }

    public MemoryCacheModelPowerpointCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public class MemoryCacheModelPdfCleanup : MemoryCacheModel
{
    public MemoryCacheModelPdfCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelPdfCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}