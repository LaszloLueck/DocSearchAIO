using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Services;
using LiteDB;
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
                            "Set Trigger for {TriggerName} in scheduler {SchedulerName} to pause because of user settings",
                            triggerName, scheduler.SchedulerName);
                        await scheduler.PauseTrigger(new TriggerKey(triggerName, groupName));
                    });
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

        public readonly Func<string, string, bool> UseExcludeFileFilter = (excludeFilter, fileName) =>
            (excludeFilter == "") || !fileName.Contains(excludeFilter);

 
        public readonly Func<IEnumerable<string>, Task<string>> CreateHashString = async (elements) =>
        {
            return await Task.Run(() =>
            {
                using var myReader = new StringReader(string.Join("", elements));
                var md5 = MD5.Create();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(myReader.ReadToEnd()));
                return Convert.ToBase64String(hash);
            });
        };

        public readonly Func<string, string, string> CreateIndexName = (mainName, suffix) => $"{mainName}-{suffix}";
    }

    public class ComparerObject
    {
        public string PathHash { get; set; }
        public string DocumentHash { get; set; }
        
        public string OriginalPath { get; set; }
    }
}