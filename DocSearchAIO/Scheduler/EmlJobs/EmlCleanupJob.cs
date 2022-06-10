using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.EmlJobs;

public class EmlCleanupJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly SchedulerUtilities _schedulerUtilities;
    private readonly ReverseComparerService<ComparerModelEml> _reverseComparerService;
    private readonly ElasticUtilities _elasticUtilities;
    private readonly JobStateMemoryCache<MemoryCacheModelEmlCleanup> _jobStateMemoryCache;
    private readonly CleanUpEntry _cleanUpEntry;

    public EmlCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache,
        ActorSystem actorSystem)
    {
        _logger = loggerFactory.CreateLogger<EmlCleanupJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _cleanUpEntry = _cfg.Cleanup[nameof(EmlCleanupDocument)];
        _schedulerUtilities = new SchedulerUtilities(loggerFactory);
        _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
        _reverseComparerService =
            new ReverseComparerService<ComparerModelEml>(loggerFactory,
                new ComparerModelEml(_cfg.ComparerDirectory), elasticSearchService, actorSystem);
        _jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetEmlCleanupJobStateMemoryCache(loggerFactory, memoryCache);
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await Task.Run(async () =>
        {
            _jobStateMemoryCache.SetCacheEntry(JobState.Running);
            if (!_cleanUpEntry.Active)
            {
                await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler, _cleanUpEntry.TriggerName,
                    _cfg.CleanupGroupName,
                    TriggerState.Paused);
                _logger.LogWarning(
                    "skip cleanup of eml-files documents because the scheduler is inactive per config");
            }
            else
            {
                await Task.Run(async () =>
                {
                    var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelEml());
                    if (cacheEntryOpt.IsSome &&
                        (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
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