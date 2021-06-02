using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using Microsoft.Extensions.Logging;
using Optional;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtils
    {
        private static ILogger _logger;
        private readonly IElasticSearchService _elasticSearchService;

        public SchedulerUtils(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtils>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task<string> CreateComparerDirectoryIfNotExists(SchedulerEntry schedulerEntry)
        {
            var compareDirectory = schedulerEntry.ComparerDirectory + schedulerEntry.ComparerFile;
            if (!File.Exists(compareDirectory))
            {
                if (!Directory.Exists(schedulerEntry.ComparerDirectory))
                {
                    _logger.LogWarning(
                        $"Comparer directory <{schedulerEntry.ComparerDirectory}> does not exist, lets create it");
                    Directory.CreateDirectory(schedulerEntry.ComparerDirectory);
                }

                _logger.LogWarning($"Comparerfile <{compareDirectory}> does not exists, lets create it");
                await File.Create(compareDirectory).DisposeAsync();
            }

            return await Task.Run(() => compareDirectory);
        }

        public void DeleteComparerFile(string compareDirectory)
        {
            _logger.LogInformation($"delete comparer file in <{compareDirectory}>");
            File.Delete(compareDirectory);
        }

        public async Task WriteAllLinesAsync(string compareDirectory, ConcurrentDictionary<string, string> comparerBag)
        {
            _logger.LogInformation($"write new comparer file in {compareDirectory}");
            await File.WriteAllLinesAsync(compareDirectory,
                comparerBag.Select(tpl => tpl.Key + ";" + tpl.Value));
        }


        public readonly Func<IScheduler, string, string, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName) =>
            {
                var currentTriggerState = await scheduler.GetTriggerState(new TriggerKey(triggerName, groupName));
                if (currentTriggerState is TriggerState.Blocked or TriggerState.Normal)
                {
                    _logger.LogWarning(
                        $"Set Trigger for {triggerName} in scheduler {scheduler.SchedulerName} to pause because of user settings!");
                    await scheduler.PauseTrigger(new TriggerKey(triggerName, groupName));
                }
            };

        public async Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument
        {
            if (!await _elasticSearchService.IndexExistsAsync(indexName))
            {
                _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                await _elasticSearchService.CreateIndexAsync<T>(indexName);
                await _elasticSearchService.RefreshIndexAsync(indexName);
                await _elasticSearchService.FlushIndexAsync(indexName);
            }
        }

        public readonly Func<string, string, bool> UseExcludeFileFilter = (excludeFilter, fileName) =>
            (excludeFilter == "") || !fileName.Contains(excludeFilter);


        public readonly Func<string, ConcurrentDictionary<string, string>> FillComparerBag = (fileName) =>
        {
            return (ConcurrentDictionary<string, string>) File.ReadAllLines(fileName).Select(str =>
            {
                var spl = str.Split(";");
                return new KeyValuePair<string, string>(spl[0], spl[1]);
            });
        };

        public async Task<Option<T>> FilterExistingUnchanged<T>(
            Option<T> document, ConcurrentDictionary<string, string> comparerBag) where T : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.FlatMap(doc =>
                {
                    var currentHash = doc.ContentHash;

                    if (!comparerBag.TryGetValue(doc.Id, out var value))
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }

                    if (currentHash == value) return Option.None<T>();
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }
                });
                return opt;
            });
        }

        public readonly Func<IEnumerable<string>, string> CreateHashString = (elements) =>
        {
            var contentString = string.Join("", elements);
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(contentString));
            return Convert.ToBase64String(hash);
        };

        public readonly Func<string, string, string> CreateIndexName = (mainName, suffix) => $"{mainName}-{suffix}";
    }
}