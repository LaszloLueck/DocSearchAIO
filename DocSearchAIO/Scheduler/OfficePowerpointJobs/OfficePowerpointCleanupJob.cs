using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficePowerpointJobs;

public class OfficePowerpointCleanupJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly ReverseComparerService<ComparerModelPowerpoint> _reverseComparerService;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly JobStateMemoryCache<MemoryCacheModelPowerpointCleanup> _jobStateMemoryCache;
    private readonly CleanUpEntry _cleanUpEntry;

    public OfficePowerpointCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<OfficePowerpointCleanupJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _cleanUpEntry = _cfg.Cleanup[nameof(PowerpointCleanupDocument)];
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _reverseComparerService =
            new ReverseComparerService<ComparerModelPowerpoint>(loggerFactory,
                new ComparerModelPowerpoint(_cfg.ComparerDirectory), elasticSearchService, actorSystem);
        _jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetPowerpointCleanupJobStateMemoryCache(loggerFactory, memoryCache);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _jobStateMemoryCache.SetCacheEntry(JobState.Running);
        if (!_cleanUpEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                _cleanUpEntry.TriggerName,
                _cfg.CleanupGroupName, TriggerState.Paused);
            _logger.LogWarning(
                "skip cleanup of powerpoint documents because the scheduler is inactive per config");
        }
        else
        {
            var cacheEntryOpt =
                _jobStateMemoryCache.CacheEntry(new MemoryCacheModelPowerpoint());
            if (cacheEntryOpt.IsSome &&
                (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
            {
                _logger.LogInformation(
                    "cannot execute cleanup documents, opponent job scanning and processing running");
                return;
            }

            _logger.LogInformation("start processing cleanup job");
            var cleanupIndexName =
                TypedIndexNameString.New(
                    _elasticUtilities.CreateIndexName(_cfg.IndexName, _cleanUpEntry.ForIndexSuffix));
            await _reverseComparerService.Process(cleanupIndexName);
        }

        _jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
    }
}