using System;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public static class JobStateMemoryCacheProxy
    {
        public static Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<WordElasticDocument>> GetWordJobStateMemoryCache =
            (loggerFactory, memoryCache) => new JobStateMemoryCache<WordElasticDocument>(loggerFactory, memoryCache);

        public static Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<PowerpointElasticDocument>>
            GetPowerpointJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<PowerpointElasticDocument>(loggerFactory, memoryCache);

        public static Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<PdfElasticDocument>>
            GetPdfJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<PdfElasticDocument>(loggerFactory, memoryCache);
    }
    
    public class JobStateMemoryCache<TModel> where TModel : ElasticDocument
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;

        public JobStateMemoryCache(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<JobStateMemoryCache<TModel>>();
            _memoryCache = memoryCache;
        }

        public Maybe<CacheEntry> GetCacheEntry()
        {
            return _memoryCache.TryGetValue(typeof(TModel).Name, out CacheEntry cacheEntry)
                ? Maybe<CacheEntry>.From(cacheEntry)
                : Maybe<CacheEntry>.None;
        }

        public void RemoveCacheEntry()
        {
            _memoryCache.Remove(typeof(TModel).Name);
        }

        public void SetCacheEntry(JobState jobState)
        {
            var cacheEntry = new CacheEntry()
                {CacheKey = typeof(TModel).Name, DateTime = DateTime.Now, JobState = jobState};
            _memoryCache.Set(cacheEntry.CacheKey, cacheEntry);
        }
    }

    public enum JobState
    {
        Running,
        Stopped
    }

    public class CacheEntry
    {
        public string CacheKey { get; set; }
        public DateTime DateTime { get; set; }
        public JobState JobState { get; set; }
    }
}