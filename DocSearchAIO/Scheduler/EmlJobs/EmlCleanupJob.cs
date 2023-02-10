using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.EmlJobs;

public class EmlCleanupJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly ActorSystem _actorSystem;
    private readonly IMemoryCache _memoryCache;

    public EmlCleanupJob(IConfigurationUpdater configurationUpdater,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache,
        ActorSystem actorSystem, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _configurationUpdater = configurationUpdater;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _memoryCache = memoryCache;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<EmlCleanupJob>();
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var cleanUpEntry = cfg.Cleanup[nameof(EmlCleanupDocument)];

        var reverseComparerService =
            new ReverseComparerService<ComparerModelEml>(new ComparerModelEml(cfg.ComparerDirectory), _elasticSearchService, _actorSystem);

        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetEmlCleanupJobStateMemoryCache(_memoryCache);
        
        jobStateMemoryCache.SetCacheEntry(JobState.Running);


        if (!cleanUpEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler, cleanUpEntry.TriggerName,
                cfg.CleanupGroupName,
                TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of eml-files documents because the scheduler is inactive per config");
        }
        else
        {
            var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelEml());
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