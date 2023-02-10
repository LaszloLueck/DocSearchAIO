using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using HtmlAgilityPack;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;
using MsgReader.Outlook;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler.MsgJobs;

[DisallowConcurrentExecution]
public class MsgProcessingJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public MsgProcessingJob(IConfigurationUpdater configurationUpdater, ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities,
        IElasticUtilities elasticUtilities)
    {
        _configurationUpdater = configurationUpdater;
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _memoryCache = memoryCache;
    }


    [Time]
    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<MsgProcessingJob>();
        
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var statisticUtilities = StatisticUtilitiesProxy.MsgStatisticUtility(TypedDirectoryPathString.New(cfg.StatisticsDirectory), new StatisticModelMsg().StatisticFileName);
        
        var comparerModel = new ComparerModelMsg(cfg.ComparerDirectory);
        
        var configEntry = cfg.Processing[nameof(MsgElasticDocument)];

        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetMsgJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();
        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelMsgCleanup());
        if (cacheEntryOpt.IsSome && (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }

        if (!configEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler, configEntry.TriggerName,
                cfg.SchedulerGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of PDF documents because the scheduler is inactive per config");
        }
        else
        {
            logger.LogInformation("start job");
            var indexName = _elasticUtilities.CreateIndexName(cfg.IndexName, configEntry.IndexSuffix);
            await _elasticUtilities.CheckAndCreateElasticIndex<MsgElasticDocument>(indexName);
            logger.LogInformation("start crunching and indexing some pdf-files");
            if (!Directory.Exists(cfg.ScanPath))
            {
                logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists, skip working", cfg.ScanPath);
            }
            else
            {
                {
                    try
                    {
                        jobStateMemoryCache.SetCacheEntry(JobState.Running);
                        var jobStatistic = new ProcessingJobStatistic
                        {
                            Id = Guid.NewGuid().ToString(),
                            StartJob = DateTime.Now
                        };

                        var sw = Stopwatch.StartNew();
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        await TypedFilePathString.New(cfg.ScanPath)
                            .CreateSource(configEntry.FileExtension)
                            .UseExcludeFileFilter(configEntry.ExcludeFilter)
                            .CountEntireDocs(statisticUtilities)
                            .ProcessedMsgDocumentAsync(configEntry, cfg, statisticUtilities, logger)
                            .FilterExistingUnchangedAsync(configEntry, comparerModel)
                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                            .WithMaybeFilter()
                            .CountFilteredDocs(statisticUtilities)
                            .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService, indexName)
                            .RunIgnoreAsync(_actorSystem.Materializer());

                        logger.LogInformation("finished processing pdf documents");
                        sw.Stop();
                        await _elasticSearchService.FlushIndexAsync(indexName);
                        await _elasticSearchService.RefreshIndexAsync(indexName);
                        jobStatistic.EndJob = DateTime.Now;
                        jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                        jobStatistic.EntireDocCount = statisticUtilities.EntireDocumentsCount();
                        jobStatistic.ProcessingError =
                            statisticUtilities.FailedDocumentsCount();
                        jobStatistic.IndexedDocCount =
                            statisticUtilities.ChangedDocumentsCount();
                        statisticUtilities.AddJobStatisticToDatabase(
                            jobStatistic);
                        comparerModel.RemoveComparerFile();
                        await comparerModel.WriteAllLinesAsync();
                        jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "An error in processing pipeline occured");
                    }
                }
            }
        }
    }
}

internal static class MsgProcessingHelper
{
    public static Source<Option<MsgElasticDocument>, NotUsed> ProcessedMsgDocumentAsync(
        this Source<string, NotUsed> source, SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelMsg> statisticUtilities, ILogger logger)
    {
        return source.SelectAsync(schedulerEntry.Parallelism,
            f => ProcessMsgDocument(f, configurationObject, statisticUtilities, logger));
    }


    private static readonly Lst<(string, string)> ToReplaced = List(
        ("&nbsp;", " "),
        (@"[^a-zA-ZäöüßÄÜÖ@.0-9]", " "),
        (Environment.NewLine, " "),
        ("[ ]{2,}", " ")
    );

    private static string RemoveCharsFromSuggest(string input) =>
        Regex.Replace(input, @"[^a-zäöüß@.]", string.Empty);

    private static async Task<Option<MsgElasticDocument>> ProcessMsgDocument(this string fileName,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelMsg> statisticUtilities,
        ILogger logger)
    {
        try
        {
            static async Task<string> GenerateBodyFromHtml(string html)
            {
                return await Task.Run(() =>
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var sb = new StringBuilder();
                    foreach (var node in doc.DocumentNode.DescendantsAndSelf())
                    {
                        if (node.HasChildNodes) continue;
                        var text = node.InnerText;
                        if (!string.IsNullOrEmpty(text) && !text.StartsWith("<!--"))
                            sb.AppendLine(text);
                    }

                    return sb.ToString();
                });
            }


            static async Task<(string Content, string Title, string Id, string Creator, IEnumerable<string>
                recipients)> ResultSet(string fileName)
            {
                return await Task.Run(async () =>
                {
                    using var msgReader = new Storage.Message(fileName);
                    var content = msgReader.BodyHtml != null
                        ? await GenerateBodyFromHtml(msgReader.BodyHtml)
                        : msgReader.BodyText;
                    var title = msgReader.Subject;
                    var id = msgReader.Id ?? Guid.NewGuid().ToString();
                    var creator = msgReader.Sender.Email;
                    var receiver =
                        msgReader.Recipients.Map(recipient => recipient.Email);
                    msgReader.Dispose();
                    return (content, title, id, creator, receiver);
                });
            }

            var resultSet = await ResultSet(fileName);

            var uriPath = fileName
                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var fileNameHash = await StaticHelpers.CreateHashString(TypedHashedInputString.New(fileName));

            var cleanContent = resultSet
                .Content
                .ReplaceSpecialStrings(ToReplaced);

            var searchAsYouTypeContent = cleanContent
                .ToLower()
                .Split(" ")
                .Map(RemoveCharsFromSuggest)
                .Distinct()
                .Filter(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                .Filter(d => d.Length() > 2);

            var completionField = new CompletionField { Input = searchAsYouTypeContent };

            var listElementsToHash = List(cleanContent, resultSet.Creator, resultSet.Title, "msg");
            var contentHash =
                (await StaticHelpers.CreateHashString(TypedHashedInputString.New(listElementsToHash.Concat()))).Value;

            return new MsgElasticDocument
            {
                OriginalFilePath = fileName,
                ContentType = "msg",
                Content = cleanContent,
                Title = resultSet.Title,
                Id = fileNameHash.Value,
                UriFilePath = uriPath,
                Creator = resultSet.Creator,
                CompletionContent = completionField,
                Receiver = resultSet.recipients,
                ProcessTime = DateTime.Now,
                ContentHash = contentHash
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occured");
            statisticUtilities.AddToFailedDocuments();
            return Option<MsgElasticDocument>.None;
        }
    }
}