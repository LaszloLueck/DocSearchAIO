using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficeWordJobs;

public class OfficeWordCleanupJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly ReverseComparerService<ComparerModelWord> _reverseComparerService;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly JobStateMemoryCache<MemoryCacheModelWordCleanup> _jobStateMemoryCache;
    private readonly CleanUpEntry _cleanUpEntry;

    public OfficeWordCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ActorSystem actorSystem,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<OfficeWordCleanupJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _cleanUpEntry = _cfg.Cleanup[nameof(WordCleanupDocument)];
        _schedulerUtilities = schedulerUtilities;
        _reverseComparerService =
            new ReverseComparerService<ComparerModelWord>(loggerFactory,
                new ComparerModelWord(_cfg.ComparerDirectory), elasticSearchService, actorSystem);
        _elasticUtilities = elasticUtilities;
        _jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetWordCleanupJobStateMemoryCache(loggerFactory, memoryCache);
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
                    "skip cleanup of word documents because the scheduler is inactive per config");
            }
            else
            {
                await Task.Run(async () =>
                {
                    var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelWord());
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
                });
            }

            _jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
        });
    }
}