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
        //private static readonly SHA256 Sha256 = SHA256.Create();

        public SchedulerUtils(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtils>();
            _elasticSearchService = elasticSearchService;
        }

        public readonly Func<SchedulerEntry, Task<string>> CreateComparerDirectoryIfNotExists = async schedulerEntry =>
        {
            var compareFilePath = schedulerEntry.ComparerDirectory + schedulerEntry.ComparerFile;
            if (File.Exists(compareFilePath)) return await Task.Run(() => compareFilePath);
            if (!Directory.Exists(schedulerEntry.ComparerDirectory))
            {
                _logger.LogWarning(
                    "comparer directory <{ComparerDirectory}> does not exist, lets create it",
                    schedulerEntry.ComparerDirectory);
                Directory.CreateDirectory(schedulerEntry.ComparerDirectory);
            }

            _logger.LogWarning("comparer file <{ComparerDirectory}> does not exists, lets create it", compareFilePath);
            await File.Create(compareFilePath).DisposeAsync();

            return await Task.Run(() => compareFilePath);
        };

        public readonly Action<string> DeleteComparerFile = compareDirectory =>
        {
            _logger.LogInformation("delete comparer file in <{ComparerDirectory}>", compareDirectory);
            File.Delete(compareDirectory);
        };


        public readonly Func<string, ConcurrentDictionary<string, string>, Task> WriteAllLinesAsync =
            async (compareDirectory, comparerBag) =>
            {
                _logger.LogInformation("write new comparer file in {ComparerDirectory}", compareDirectory);
                await File.WriteAllLinesAsync(compareDirectory,
                    comparerBag.Select(tpl => tpl.Key + ";" + tpl.Value));
            };


        public readonly Func<IScheduler, string, string, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName) =>
            {
                var currentTriggerState = await scheduler.GetTriggerState(new TriggerKey(triggerName, groupName));
                if (currentTriggerState is TriggerState.Blocked or TriggerState.Normal)
                {
                    _logger.LogWarning(
                        "Set Trigger for {TriggerName} in scheduler {SchedulerName} to pause because of user settings",
                        triggerName, scheduler.SchedulerName);
                    await scheduler.PauseTrigger(new TriggerKey(triggerName, groupName));
                }
            };

        public async Task CheckAndCreateElasticIndex<T>(string indexName) where T : ElasticDocument
        {
            if (await _elasticSearchService.IndexExistsAsync(indexName))
                return;
            _logger.LogInformation("Index {IndexName} does not exist, lets create them", indexName);
            await _elasticSearchService.CreateIndexAsync<T>(indexName);
            await _elasticSearchService.RefreshIndexAsync(indexName);
            await _elasticSearchService.FlushIndexAsync(indexName);
        }

        public readonly Func<string, string, bool> UseExcludeFileFilter = (excludeFilter, fileName) =>
            (excludeFilter == "") || !fileName.Contains(excludeFilter);


        public readonly Func<string, ConcurrentDictionary<string, string>> FillComparerBag = fileName =>
        {
            var result = File.ReadAllLines(fileName).Select(str =>
            {
                var spl = str.Split(";");
                return new KeyValuePair<string, string>(spl[0], spl[1]);
            });
            var hashPairs = result as KeyValuePair<string, string>[] ?? result.ToArray();
            return hashPairs.Any()
                ? new ConcurrentDictionary<string, string>(hashPairs.ToDictionary(element => element.Key,
                    element => element.Value))
                : new ConcurrentDictionary<string, string>();
        };

        public static async Task<Option<T>> FilterExistingUnchanged<T>(
            Option<T> document, ConcurrentDictionary<string, string> comparerBag) where T : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.FlatMap(doc =>
                {
                    var currentHash = doc.ContentHash;

                    if (!comparerBag.TryGetValue(doc.Id, out var value))
                    {
                        _logger.LogInformation("document not in bag: {NotInBag}", doc.OriginalFilePath);
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (_, _) => currentHash);
                        return Option.Some(doc);
                    }

                    if (currentHash == value) return Option.None<T>();
                    {
                        _logger.LogInformation("changed document: {ChangedDocument}", doc.OriginalFilePath);
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (_, _) => currentHash);
                        return Option.Some(doc);
                    }
                });
                return opt;
            });
        }

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
}