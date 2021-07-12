using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public class MemoryCacheElement
    {
        public readonly Func<MemoryCacheModel> Element;

        public MemoryCacheElement(Func<MemoryCacheModel> element)
        {
            Element = element;
        }
    }

    public class MemoryCacheModelProxy
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheModelProxy(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
        {
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
        }

        public IEnumerable<KeyValuePair<string, MemoryCacheElement>> GetModels()
        {
            return new[]
            {
                KeyValuePair.Create("wordProcessingJob",
                    new MemoryCacheElement(() => new MemoryCacheModelWord(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("excelProcessingJob",
                    new MemoryCacheElement(() => new MemoryCacheModelExcel(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("pdfProcessingJob",
                    new MemoryCacheElement(() => new MemoryCacheModelPdf(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("powerpointProcessingJob",
                    new MemoryCacheElement(() => new MemoryCacheModelPowerpoint(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("wordCleanupJob",
                    new MemoryCacheElement(() => new MemoryCacheModelWordCleanup(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("excelCleanupJob",
                    new MemoryCacheElement(() => new MemoryCacheModelExcelCleanup(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("powerpointCleanupJob",
                    new MemoryCacheElement(() => new MemoryCacheModelPowerpointCleanup(_loggerFactory, _memoryCache))),
                KeyValuePair.Create("pdfCleanupJob",
                    new MemoryCacheElement(() => new MemoryCacheModelPdfCleanup(_loggerFactory, _memoryCache)))
            };
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

        public Maybe<CacheEntry> CacheEntry()
        {
            _logger.LogInformation("try to get cache entry for model {DerivedModelName}", DerivedModelName);
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
}