using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Services;
using LiteDB;
using Microsoft.Extensions.Logging;
using Optional;
using Optional.Collections;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtils
    {
        private static ILogger _logger;

        private readonly IElasticSearchService _elasticSearchService;
        private readonly ILiteCollection<ComparerObject> _col;

        public SchedulerUtils(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtils>();
            _elasticSearchService = elasticSearchService;
            _col = liteDatabase.GetCollection<ComparerObject>("comparers");
            _col.EnsureIndex(x => x.PathHash);
        }

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


        public async Task<Option<T>> FilterExistingUnchanged<T>(
            Option<T> document) where T : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.FlatMap(doc =>
                {
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    return _col.FindOne(comp => comp.PathHash == pathHash).SomeNotNull().Map(innerDoc =>
                    {
                        if (innerDoc.DocumentHash == contentHash)
                            return Option.None<T>();

                        innerDoc.DocumentHash = contentHash;
                        _col.Update(innerDoc);
                        return Option.Some(doc);
                    }).ValueOr(() =>
                    {
                        var innerDocument = new ComparerObject()
                        {
                            DocumentHash = contentHash,
                            PathHash = pathHash
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
    }
    
    public static class Helpers
    {
        public static Option<TOut> AsOptionalValue<TOut>(this bool source, Func<TOut> action)
        {
            return source.SomeWhen(t => t).Map(_ => action.Invoke());
        }

        public static void Either<TIn>(this bool source, TIn parameter, Action<TIn> left, Action<TIn> right)
        {
            if (source)
            {
                right.Invoke(parameter);
            }
            else
            {
                left.Invoke(parameter);
            }
        }

        public static void DirectoryNotExistsAction(this string path, Action<string> action)
        {
            if (!Directory.Exists(path))
                action.Invoke(path);
        }

        public static Source<IEnumerable<TSource>, TMat> WithOptionFilter<TSource, TMat>(this Source<IEnumerable<Option<TSource>>, TMat> source)
        {
            return source.Select(d => d.Values());
        }
        
        
    }
    
}