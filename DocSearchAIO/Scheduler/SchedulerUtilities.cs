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
using Optional;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtilities
    {
        private static ILogger _logger;

        private readonly IElasticSearchService _elasticSearchService;
        private readonly ILiteCollection<ComparerObject> _col;

        public SchedulerUtilities(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService,
            ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtilities>();
            _elasticSearchService = elasticSearchService;
            _col = liteDatabase.GetCollection<ComparerObject>("comparers");
            _col.EnsureIndex(x => x.PathHash);
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


        public async Task<Maybe<T>> FilterExistingUnchanged<T>(Maybe<T> document) where T : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.Bind(doc =>
                {
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    var originalFilePath = doc.OriginalFilePath;
                    return _col
                        .FindOne(comp => comp.PathHash == pathHash)
                        .MaybeValue()
                        .ResolveOr(
                            innerDoc =>
                            {
                                if (innerDoc.DocumentHash == contentHash)
                                    return Maybe<T>.None;

                                innerDoc.DocumentHash = contentHash;
                                _col.Update(innerDoc);
                                return Maybe<T>.From(doc);
                            },
                            () =>
                            {
                                var innerDocument = new ComparerObject
                                {
                                    DocumentHash = contentHash,
                                    PathHash = pathHash,
                                    OriginalPath = originalFilePath
                                };
                                _col.Insert(innerDocument);
                                return Maybe<T>.From(doc);
                            });
                });
                return opt;
            });
        }

        public async Task<Option<T>> FilterExistingUnchanged<T>(
            Option<T> document) where T : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.FlatMap(doc =>
                {
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    var originalFilePath = doc.OriginalFilePath;
                    return _col
                        .FindOne(comp => comp.PathHash == pathHash)
                        .SomeNotNull()
                        .Map(innerDoc =>
                        {
                            if (innerDoc.DocumentHash == contentHash)
                                return Option.None<T>();

                            innerDoc.DocumentHash = contentHash;
                            _col.Update(innerDoc);
                            return Option.Some(doc);
                        })
                        .ValueOr(() =>
                        {
                            var innerDocument = new ComparerObject
                            {
                                DocumentHash = contentHash,
                                PathHash = pathHash,
                                OriginalPath = originalFilePath
                            };
                            _col.Insert(innerDocument);
                            return Option.Some(doc);
                        });
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

    public class ComparerObject
    {
        public string PathHash { get; set; }
        public string DocumentHash { get; set; }
        
        public string OriginalPath { get; set; }
    }
}