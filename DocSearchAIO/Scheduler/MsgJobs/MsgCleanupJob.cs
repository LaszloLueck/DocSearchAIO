using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.MsgJobs;

public class MsgCleanupJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ActorSystem _actorSystem;
    private readonly IMemoryCache _memoryCache;

    public MsgCleanupJob(IConfigurationUpdater configurationUpdater,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache,
        ActorSystem actorSystem, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _actorSystem = actorSystem;
        _configurationUpdater = configurationUpdater;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _elasticSearchService = elasticSearchService;
        _memoryCache = memoryCache;
    }


    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<MsgCleanupJob>();
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var cleanUpEntry = cfg.Cleanup[nameof(MsgCleanupDocument)];
        var reverseComparerService =
            new ReverseComparerService<ComparerModelMsg>(new ComparerModelMsg(cfg.ComparerDirectory), _elasticSearchService, _actorSystem);

        var jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetMsgCleanupJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.SetCacheEntry(JobState.Running);
        if (!cleanUpEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler, cleanUpEntry.TriggerName,
                cfg.CleanupGroupName,
                TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of msg-files documents because the scheduler is inactive per config");
        }
        else
        {
            var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelMsg());
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