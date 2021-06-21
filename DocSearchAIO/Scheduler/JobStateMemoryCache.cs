using System;
using System.Collections.Generic;
using DocSearchAIO.Classes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public static class JobStateMemoryCacheProxy
    {
        public static readonly Func<ILoggerFactory, IMemoryCache,
            IEnumerable<KeyValuePair<ProcessorBase, Func<MemoryCacheModel>>>>
            AsIEnumerable = (loggerFactory, memoryCache) =>
            {
                return new[]
                {
                    KeyValuePair.Create<ProcessorBase, Func<MemoryCacheModel>>(
                        new ProcessorBaseWord(),
                        () => new MemoryCacheModelWord(loggerFactory, memoryCache)),
                    KeyValuePair.Create<ProcessorBase, Func<MemoryCacheModel>>(
                        new ProcessorBasePowerpoint(),
                        () => new MemoryCacheModelPowerpoint(loggerFactory, memoryCache)),
                    KeyValuePair.Create<ProcessorBase, Func<MemoryCacheModel>>(
                        new ProcessorBasePdf(),
                        () => new MemoryCacheModelPdf(loggerFactory, memoryCache))
                };
            };

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelWord>>
            GetWordJobStateMemoryCache =
                (loggerFactory, memoryCache) => new JobStateMemoryCache<MemoryCacheModelWord>(loggerFactory, memoryCache);

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPowerpoint>>
            GetPowerpointJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<MemoryCacheModelPowerpoint>(loggerFactory, memoryCache);

        public static readonly Func<ILoggerFactory, IMemoryCache, JobStateMemoryCache<MemoryCacheModelPdf>>
            GetPdfJobStateMemoryCache = (loggerFactory, memoryCache) =>
                new JobStateMemoryCache<MemoryCacheModelPdf>(loggerFactory, memoryCache);
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
        
        public void RemoveCacheEntry()
        {
            _logger.LogInformation("remove cache entry");
            _memoryCache.Remove(typeof(TModel).Name);
        }

        public void SetCacheEntry(JobState jobState)
        {
            _logger.LogInformation($"set cache entry to {jobState}");
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