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

        public Maybe<CacheEntry> GetCacheEntry()
        {
            _logger.LogInformation("try to get cache entry");
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

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPowerpoint : MemoryCacheModel
    {
        public MemoryCacheModelPowerpoint(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }

        protected override string DerivedModelName => GetType().Name;
    }

    public class MemoryCacheModelPdf : MemoryCacheModel
    {
        public MemoryCacheModelPdf(ILoggerFactory loggerFactory, IMemoryCache memoryCache) : base(loggerFactory, memoryCache)
        {
        }

        protected override string DerivedModelName => GetType().Name;
    }
}