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

    public Seq<(ProcessingJobType, Func<MemoryCacheModel>)> Models()
    {
        return Seq<(ProcessingJobType, Func<MemoryCacheModel>)>(
            (ProcessingJobType.WordProcessingJobType, () => new MemoryCacheModelWord(_loggerFactory, _memoryCache)),
            (ProcessingJobType.ExcelProcessingJobType, () => new MemoryCacheModelExcel(_loggerFactory, _memoryCache)),
            (ProcessingJobType.PdfProcessingJobType, () => new MemoryCacheModelPdf(_loggerFactory, _memoryCache)),
            (ProcessingJobType.PowerpointProcessingJobType,
                () => new MemoryCacheModelPowerpoint(_loggerFactory, _memoryCache)),
            (ProcessingJobType.WordCleanupJobType, () => new MemoryCacheModelWordCleanup(_loggerFactory, _memoryCache)),
            (ProcessingJobType.ExcelCleanupJobType,
                () => new MemoryCacheModelExcelCleanup(_loggerFactory, _memoryCache)),
            (ProcessingJobType.PowerpointCleanupJobType,
                () => new MemoryCacheModelPowerpointCleanup(_loggerFactory, _memoryCache)),
            (ProcessingJobType.PdfCleanupJobType, () => new MemoryCacheModelPdfCleanup(_loggerFactory, _memoryCache)));
    }
}

public sealed class ProcessingJobType
{
    private readonly string _name;

    private ProcessingJobType(string name)
    {
        _name = name;
    }

    public static readonly ProcessingJobType WordProcessingJobType = new("wordProcessingJob");
    public static readonly ProcessingJobType ExcelProcessingJobType = new("excelProcessingJob");
    public static readonly ProcessingJobType PowerpointProcessingJobType = new("powerpointProcessingJob");
    public static readonly ProcessingJobType PdfProcessingJobType = new("pdfProcessingJob");
    public static readonly ProcessingJobType WordCleanupJobType = new("wordCleanupJob");
    public static readonly ProcessingJobType ExcelCleanupJobType = new("excelCleanupJob");
    public static readonly ProcessingJobType PowerpointCleanupJobType = new("powerpointCleanupJob");
    public static readonly ProcessingJobType PdfCleanupJobType = new("pdfCleanupJob");

    public override string ToString()
    {
        return _name;
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
        if (_logger is null || _memoryCache is null)
            return Option<CacheEntry>.None;

        _logger.LogInformation("try to get cache entry for model {DerivedModelName}", DerivedModelName);
        _memoryCache.TryGetValue(DerivedModelName, out CacheEntry? cacheEntry);
        return cacheEntry;
    }
}

public sealed class MemoryCacheModelWord : MemoryCacheModel
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

public sealed class MemoryCacheModelPowerpoint : MemoryCacheModel
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

public sealed class MemoryCacheModelPdf : MemoryCacheModel
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

public sealed class MemoryCacheModelExcel : MemoryCacheModel
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

public sealed class MemoryCacheModelExcelCleanup : MemoryCacheModel
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

public sealed class MemoryCacheModelMsg : MemoryCacheModel
{
    protected override string DerivedModelName => GetType().Name;

    public MemoryCacheModelMsg(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelMsg()
    {
    }
}

public sealed class MemoryCacheModelMsgCleanup : MemoryCacheModel
{
    public MemoryCacheModelMsgCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelMsgCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelEml : MemoryCacheModel
{
    protected override string DerivedModelName => GetType().Name;

    public MemoryCacheModelEml(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelEml()
    {
    }
}

public sealed class MemoryCacheModelEmlCleanup : MemoryCacheModel
{
    public MemoryCacheModelEmlCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory,
        memoryCache)
    {
    }

    public MemoryCacheModelEmlCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelWordCleanup : MemoryCacheModel
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

public sealed class MemoryCacheModelPowerpointCleanup : MemoryCacheModel
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

public sealed class MemoryCacheModelPdfCleanup : MemoryCacheModel
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