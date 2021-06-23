using System;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Services;
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

        public readonly Func<IScheduler, string, string, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName) =>
            {
                var currentTriggerState = await scheduler.GetTriggerState(new TriggerKey(triggerName, groupName));
                (currentTriggerState is TriggerState.Blocked or TriggerState.Normal)
                    .IfTrue(async () =>
                    {
                        _logger.LogWarning(
                            $"Set Trigger for {triggerName} in scheduler {scheduler.SchedulerName} to pause because of user settings");
                        await scheduler.PauseTrigger(new TriggerKey(triggerName, groupName));
                    });
            };

        public async Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument
        {
            (!await _elasticSearchService.IndexExistsAsync(indexName))
                .IfTrue(async () =>
                {
                    _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                    await _elasticSearchService.CreateIndexAsync<T>(indexName);
                    await _elasticSearchService.RefreshIndexAsync(indexName);
                    await _elasticSearchService.FlushIndexAsync(indexName);
                });
        }

        // public readonly Func<string, string, bool> UseExcludeFileFilter = (excludeFilter, fileName) =>
        //     excludeFilter == "" || !fileName.Contains(excludeFilter);
        

        public readonly Func<string, string, string> CreateIndexName = (mainName, suffix) => $"{mainName}-{suffix}";
    }

    public class ComparerObject
    {
        public string PathHash { get; set; }
        public string DocumentHash { get; set; }
        
        public string OriginalPath { get; set; }
    }
}