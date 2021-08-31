using System.Threading.Tasks;
using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficeExcelJobs
{
    public class OfficeExcelCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ReverseComparerService<ComparerModelExcel> _reverseComparerService;
        private readonly ElasticUtilities _elasticUtilities;
        private readonly JobStateMemoryCache<MemoryCacheModelExcelCleanup> _jobStateMemoryCache;
        private readonly CleanUpEntry _cleanUpEntry;

        public OfficeExcelCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<OfficeExcelCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _cleanUpEntry = _cfg.Cleanup[nameof(ExcelCleanupDocument)];
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _reverseComparerService =
                new ReverseComparerService<ComparerModelExcel>(loggerFactory,
                    new ComparerModelExcel(_cfg.ComparerDirectory), elasticSearchService, actorSystem);
            _jobStateMemoryCache =
                JobStateMemoryCacheProxy.GetExcelCleanupJobStateMemoryCache(loggerFactory, memoryCache);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                if (!_cleanUpEntry.Active)
                {
                    await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                        _cleanUpEntry.TriggerName,
                        _cfg.CleanupGroupName, TriggerState.Paused);
                    _logger.LogWarning(
                        "skip cleanup of excel documents because the scheduler is inactive per config");
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelExcel());
                        if (!cacheEntryOpt.HasNoValue &&
                            (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                        {
                            _logger.LogInformation(
                                "cannot execute cleanup documents, opponent job scanning and processing running");
                            return;
                        }

                        _logger.LogInformation("start processing cleanup job");
                        var cleanupIndexName =
                            _elasticUtilities.CreateIndexName(_cfg.IndexName, _cleanUpEntry.ForIndexSuffix);
                        await _reverseComparerService.Process(cleanupIndexName);
                    });
                }

                _jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
            });
        }
    }
}