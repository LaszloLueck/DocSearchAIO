using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficeWordJobs;

public class OfficeWordCleanupJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public OfficeWordCleanupJob(IConfigurationUpdater configurationUpdater,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _configurationUpdater = configurationUpdater;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _memoryCache = memoryCache;
        _actorSystem = actorSystem;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<OfficeWordCleanupJob>();
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var cleanUpEntry = cfg.Cleanup[nameof(WordCleanupDocument)];

        var reverseComparerService =
            new ReverseComparerService<ComparerModelWord>(new ComparerModelWord(cfg.ComparerDirectory),
                _elasticSearchService, _actorSystem);
        var jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetWordCleanupJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.SetCacheEntry(JobState.Running);
        if (!cleanUpEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                cleanUpEntry.TriggerName,
                cfg.CleanupGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of word documents because the scheduler is inactive per config");
        }
        else
        {
            var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelWord());
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