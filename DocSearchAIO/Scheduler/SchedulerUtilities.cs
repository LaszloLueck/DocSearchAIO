using System;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtilities
    {
        private static ILogger _logger;

        private readonly IElasticSearchService _elasticSearchService;


        public SchedulerUtilities(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtilities>();
            _elasticSearchService = elasticSearchService;
        }

        public readonly Func<IScheduler, string, string, TriggerState, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName, triggerState) =>
            {
                var currentTriggerKey = new TriggerKey(triggerName, groupName);
                var currentTriggerState = await scheduler.GetTriggerState(currentTriggerKey);
                _logger.LogInformation("current triggerstate is {Triggerstate}", currentTriggerState);
                _logger.LogInformation($"set triggerstate for trigger {triggerName} to {triggerState}");
                switch (triggerState)
                {
                    case TriggerState.Paused: 
                        await scheduler.PauseTrigger(currentTriggerKey);
                        break;
                    case TriggerState.Normal: 
                        await scheduler.ResumeTrigger(currentTriggerKey);
                        break;
                    case TriggerState.Complete:
                        break;
                    case TriggerState.Error:
                        break;
                    case TriggerState.Blocked:
                        break;
                    case TriggerState.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(triggerState), triggerState, "cannot process with that trigger state");
                }

                // (currentTriggerState is TriggerState.Blocked or TriggerState.Normal)
                //     .IfTrue(async () =>
                //     {
                //         _logger.LogWarning(
                //             "Set Trigger for {TriggerName} in scheduler {SchedulerName} to pause because of user settings",
                //             triggerName, scheduler.SchedulerName);
                //         await scheduler.PauseTrigger(new TriggerKey(triggerName, groupName));
                //     });
            };

        public async Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument
        {
            (!await _elasticSearchService.IndexExistsAsync(indexName))
                .IfTrue(async () =>
                {
                    _logger.LogInformation("Index {IndexName} does not exist, lets create them", indexName);
                    await _elasticSearchService.CreateIndexAsync<T>(indexName);
                    await _elasticSearchService.RefreshIndexAsync(indexName);
                    await _elasticSearchService.FlushIndexAsync(indexName);
                });
        }

        public readonly Func<string, string, string> CreateIndexName = (mainName, suffix) => $"{mainName}-{suffix}";
    }

    public class ComparerObject
    {
        public string PathHash { get; set; }
        public string DocumentHash { get; set; }

        public string OriginalPath { get; set; }
    }
}