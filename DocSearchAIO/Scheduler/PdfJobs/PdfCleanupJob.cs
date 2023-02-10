using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.PdfJobs;

public class PdfCleanupJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ActorSystem _actorSystem;
    private readonly IMemoryCache _memoryCache;

    public PdfCleanupJob(IConfigurationUpdater configurationUpdater,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _configurationUpdater = configurationUpdater;
        _elasticSearchService = elasticSearchService;
        _actorSystem = actorSystem;
        _memoryCache = memoryCache;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<PdfCleanupJob>();
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var cleanUpEntry = cfg.Cleanup[nameof(PdfCleanupDocument)];
        var reverseComparerService =
            new ReverseComparerService<ComparerModelPdf>(new ComparerModelPdf(cfg.ComparerDirectory), _elasticSearchService, _actorSystem);
        var jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetPdfCleanupJobStateMemoryCache(_memoryCache);

        jobStateMemoryCache.SetCacheEntry(JobState.Running);
        if (!cleanUpEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                cleanUpEntry.TriggerName,
                cfg.CleanupGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of pdf documents because the scheduler is inactive per config");
        }
        else
        {
            var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelPdf());
            if (cacheEntryOpt.IsSome &&
                (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
            {
                logger.LogInformation(
                    "cannot execute cleanup documents, opponent job scanning and processing running");
                return;
            }

            logger.LogInformation("start processing cleanup job");
            var cleanupIndexName =
                TypedIndexNameString.New(
                    _elasticUtilities.CreateIndexName(cfg.IndexName, cleanUpEntry.ForIndexSuffix));
            await reverseComparerService.Process(cleanupIndexName);
        }

        jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
    }
}