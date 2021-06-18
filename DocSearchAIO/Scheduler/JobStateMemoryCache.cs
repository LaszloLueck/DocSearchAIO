using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public static class JobStateMemoryCacheProxy
    {
        public static readonly Func<ILoggerFactory, IMemoryCache,
                IEnumerable<KeyValuePair<IProcessorType, Func<Maybe<CacheEntry>>>>>
            AsIEnumerable = (loggerFactory, memoryCache) =>
            {
                return new[]
                {
                    KeyValuePair.Create<IProcessorType, Func<Maybe<CacheEntry>>>(
                        new ProcessorTypeWord(),
                        () => GetWordJobStateMemoryCache(loggerFactory, memoryCache).GetCacheEntry()),
                    KeyValuePair.Create<IProcessorType, Func<Maybe<CacheEntry>>>(
                        new ProcessorTypePowerPoint(),
                        () => GetPowerpointJobStateMemoryCache(loggerFactory, memoryCache).GetCacheEntry()),
                    KeyValuePair.Create<IProcessorType, Func<Maybe<CacheEntry>>>(
                        new ProcessorTypePdf(),
                        () => GetPdfJobStateMemoryCache(loggerFactory, memoryCache).GetCacheEntry())
                };
            };

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<ProcessorTypeWord>>
            GetWordJobStateMemoryCache =
                (loggerFactory, memoryCache) => new JobStateMemoryCache<ProcessorTypeWord>(loggerFactory, memoryCache);

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<ProcessorTypePowerPoint>>
            GetPowerpointJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<ProcessorTypePowerPoint>(loggerFactory, memoryCache);

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<ProcessorTypePdf>>
            GetPdfJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<ProcessorTypePdf>(loggerFactory, memoryCache);
    }

    public class JobStateMemoryCache<TModel> where TModel : IProcessorType
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
            _logger.LogInformation("try to get cache entry");
            return _memoryCache.TryGetValue(typeof(TModel).Name, out CacheEntry cacheEntry)
                ? Maybe<CacheEntry>.From(cacheEntry)
                : Maybe<CacheEntry>.None;
        }

        public void RemoveCacheEntry()
        {
            _logger.LogInformation("remove cache entry");
            _memoryCache.Remove(typeof(TModel).Name);
        }

        public void SetCacheEntry(JobState jobState)
        {
            _logger.LogInformation("set cache entry to {State}", jobState);
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