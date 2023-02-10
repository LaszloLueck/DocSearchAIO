using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Scheduler;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.Classes;

public class MemoryCacheModelProxy
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheModelProxy(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Seq<(ProcessingJobType, Func<MemoryCacheModel>)> Models()
    {
        return Seq<(ProcessingJobType, Func<MemoryCacheModel>)>(
            (ProcessingJobType.WordProcessingJobType, () => new MemoryCacheModelWord(_memoryCache)),
            (ProcessingJobType.ExcelProcessingJobType, () => new MemoryCacheModelExcel(_memoryCache)),
            (ProcessingJobType.PdfProcessingJobType, () => new MemoryCacheModelPdf(_memoryCache)),
            (ProcessingJobType.PowerpointProcessingJobType,
                () => new MemoryCacheModelPowerpoint(_memoryCache)),
            (ProcessingJobType.WordCleanupJobType, () => new MemoryCacheModelWordCleanup(_memoryCache)),
            (ProcessingJobType.ExcelCleanupJobType,
                () => new MemoryCacheModelExcelCleanup(_memoryCache)),
            (ProcessingJobType.PowerpointCleanupJobType,
                () => new MemoryCacheModelPowerpointCleanup(_memoryCache)),
            (ProcessingJobType.PdfCleanupJobType, () => new MemoryCacheModelPdfCleanup(_memoryCache)));
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

    protected MemoryCacheModel(IMemoryCache memoryCache)
    {
        _logger = LoggingFactoryBuilder.Build<MemoryCacheModel>();
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
    public MemoryCacheModelWord(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelWord()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelPowerpoint : MemoryCacheModel
{
    public MemoryCacheModelPowerpoint(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelPowerpoint()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelPdf : MemoryCacheModel
{
    public MemoryCacheModelPdf(IMemoryCache memoryCache) : base(memoryCache)
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

    public MemoryCacheModelExcel(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelExcel()
    {
    }
}

public sealed class MemoryCacheModelExcelCleanup : MemoryCacheModel
{
    public MemoryCacheModelExcelCleanup(IMemoryCache memoryCache) : base(memoryCache)
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

    public MemoryCacheModelMsg(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelMsg()
    {
    }
}

public sealed class MemoryCacheModelMsgCleanup : MemoryCacheModel
{
    public MemoryCacheModelMsgCleanup(IMemoryCache memoryCache) : base(memoryCache)
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

    public MemoryCacheModelEml(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelEml()
    {
    }
}

public sealed class MemoryCacheModelEmlCleanup : MemoryCacheModel
{
    public MemoryCacheModelEmlCleanup(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelEmlCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelWordCleanup : MemoryCacheModel
{
    public MemoryCacheModelWordCleanup(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelWordCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelPowerpointCleanup : MemoryCacheModel
{
    public MemoryCacheModelPowerpointCleanup(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelPowerpointCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}

public sealed class MemoryCacheModelPdfCleanup : MemoryCacheModel
{
    public MemoryCacheModelPdfCleanup(IMemoryCache memoryCache) : base(memoryCache)
    {
    }

    public MemoryCacheModelPdfCleanup()
    {
    }

    protected override string DerivedModelName => GetType().Name;
}