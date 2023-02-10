using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.EmlJobs;

public class EmlProcessingJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IMemoryCache _memoryCache;

    public EmlProcessingJob(IConfigurationUpdater configurationUpdater,
        ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities,
        IElasticUtilities elasticUtilities)
    {
        _configurationUpdater = configurationUpdater;
        _actorSystem = actorSystem;
        _schedulerUtilities = schedulerUtilities;
        _elasticSearchService = elasticSearchService;
        _elasticUtilities = elasticUtilities;
        _memoryCache = memoryCache;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<EmlProcessingJob>();
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var statisticUtilities = StatisticUtilitiesProxy.EmlStatisticUtility(
            TypedDirectoryPathString.New(cfg.StatisticsDirectory), new StatisticModelEml().StatisticFileName);
        var comparerModel = new ComparerModelEml(cfg.ComparerDirectory);

        var configEntry = cfg.Processing[nameof(EmlElasticDocument)];
        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetEmlJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();
        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelEmlCleanup());
        if (cacheEntryOpt.IsSome && (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            logger.LogInformation("cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }
    }
}