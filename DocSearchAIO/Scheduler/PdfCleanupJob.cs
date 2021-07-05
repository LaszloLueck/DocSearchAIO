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

namespace DocSearchAIO.Scheduler
{
    public class PdfCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ElasticUtilities _elasticUtilities;
        private readonly ReverseComparerService<ComparerModelPdf> _reverseComparerService;
        private readonly JobStateMemoryCache<MemoryCacheModelPdfCleanup> _jobStateMemoryCache;
        private readonly CleanUpEntry _cleanUpEntry;

        public PdfCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<PdfCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _cleanUpEntry = _cfg.Cleanup[nameof(PdfCleanupDocument)];
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _reverseComparerService =
                new ReverseComparerService<ComparerModelPdf>(loggerFactory,
                    new ComparerModelPdf(_cfg.ComparerDirectory), elasticSearchService, actorSystem);
            _jobStateMemoryCache =
                JobStateMemoryCacheProxy.GetPdfCleanupJobStateMemoryCache(loggerFactory, memoryCache);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                _cleanUpEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                _cleanUpEntry.TriggerName,
                                _cfg.CleanupGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of pdf documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            await Task.Run(async () =>
                            {
                                var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelPdf());
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
                    );
                _jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
            });
        }
    }
}