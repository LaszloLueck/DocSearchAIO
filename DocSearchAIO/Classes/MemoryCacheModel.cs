using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public abstract class MemoryCacheModel
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;

        protected abstract string DerivedModelName { get; }

        protected MemoryCacheModel(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<MemoryCacheModel>();
            _memoryCache = memoryCache;
        }
        
        protected MemoryCacheModel(){}

        public Maybe<CacheEntry> GetCacheEntry()
        {
            _logger.LogInformation("try to get cache entry for model {DerivedModelName}", DerivedModelName);
            return _memoryCache.TryGetValue(DerivedModelName, out CacheEntry cacheEntry)
                ? Maybe<CacheEntry>.From(cacheEntry)
                : Maybe<CacheEntry>.None;
        }

    }

    public class MemoryCacheModelWord : MemoryCacheModel
    {
        public MemoryCacheModelWord(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelWord(){}
        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPowerpoint : MemoryCacheModel
    {
        public MemoryCacheModelPowerpoint(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelPowerpoint(){}

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPdf : MemoryCacheModel
    {
        public MemoryCacheModelPdf(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelPdf(){}

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelExcel : MemoryCacheModel
    {
        protected override string DerivedModelName => GetType().Name;
        public MemoryCacheModelExcel(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelExcel(){}
        
    }

    public class MemoryCacheModelExcelCleanup : MemoryCacheModel
    {
        public MemoryCacheModelExcelCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }

        public MemoryCacheModelExcelCleanup(){}
        
        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelWordCleanup : MemoryCacheModel
    {
        public MemoryCacheModelWordCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelWordCleanup(){}

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPowerpointCleanup : MemoryCacheModel
    {
        public MemoryCacheModelPowerpointCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }
        
        public MemoryCacheModelPowerpointCleanup(){}

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPdfCleanup : MemoryCacheModel
    {
        public MemoryCacheModelPdfCleanup(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }

        public MemoryCacheModelPdfCleanup(){}
        
        protected override string DerivedModelName => GetType().Name;
    }
    
    
}