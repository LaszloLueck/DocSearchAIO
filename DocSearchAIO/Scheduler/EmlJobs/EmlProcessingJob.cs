using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.EmlJobs;

public class EmlProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelEml> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelEml> _jobStateMemoryCache;
    private readonly IElasticUtilities _elasticUtilities;

    public EmlProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities,
        IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<EmlProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _statisticUtilities = StatisticUtilitiesProxy.EmlStatisticUtility(loggerFactory,
            TypedDirectoryPathString.New(_cfg.StatisticsDirectory), new StatisticModelEml().StatisticFileName);
        _comparerModel = new ComparerModelEml(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache = JobStateMemoryCacheProxy.GetEmlJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }

    public async Task Execute(IJobExecutionContext context)
    {

        await Task.Run(() =>
        {
            var configEntry = _cfg.Processing[nameof(EmlElasticDocument)];
            var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelEmlCleanup());
            if (cacheEntryOpt.IsSome && (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
            {
                _logger.LogInformation("cannot execute scanning and processing documents, opponent job cleanup running");
                return;
            }
        });


    }
}