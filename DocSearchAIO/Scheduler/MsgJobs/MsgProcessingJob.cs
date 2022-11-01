using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using MsgReader.Outlook;
using Nest;
using Quartz;
using Quartz.Util;

namespace DocSearchAIO.Scheduler.MsgJobs;

[DisallowConcurrentExecution]
public class MsgProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelMsg> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelMsg> _jobStateMemoryCache;
    private readonly IElasticUtilities _elasticUtilities;


    public MsgProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities,
        IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<MsgProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _statisticUtilities = StatisticUtilitiesProxy.MsgStatisticUtility(loggerFactory,
            TypedDirectoryPathString.New(_cfg.StatisticsDirectory), new StatisticModelMsg().StatisticFileName);
        _comparerModel = new ComparerModelMsg(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache = JobStateMemoryCacheProxy.GetMsgJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }


    public async Task Execute(IJobExecutionContext context)
    {
        var configEntry = _cfg.Processing[nameof(MsgElasticDocument)];
        var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelMsgCleanup());
        if (cacheEntryOpt.IsSome && (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            _logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }

        if (!configEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler, configEntry.TriggerName,
                _cfg.SchedulerGroupName, TriggerState.Paused);
            _logger.LogWarning(
                "skip cleanup of PDF documents because the scheduler is inactive per config");
        }
        else
        {
            _logger.LogInformation("start job");
            var indexName = _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);
            await _elasticUtilities.CheckAndCreateElasticIndex<MsgElasticDocument>(indexName);
            _logger.LogInformation("start crunching and indexing some pdf-files");
            if (!Directory.Exists(_cfg.ScanPath))
            {
                _logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists, skip working", _cfg.ScanPath);
            }
            else
            {
                {
                    try
                    {
                        _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                        var jobStatistic = new ProcessingJobStatistic
                        {
                            Id = Guid.NewGuid().ToString(),
                            StartJob = DateTime.Now
                        };

                        var sw = Stopwatch.StartNew();
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        await TypedFilePathString.New(_cfg.ScanPath)
                            .CreateSource(configEntry.FileExtension)
                            .UseExcludeFileFilter(configEntry.ExcludeFilter)
                            .CountEntireDocs(_statisticUtilities)
                            .ProcessedMsgDocumentAsync(configEntry, _cfg, _statisticUtilities, _logger)
                            .FilterExistingUnchangedAsync(configEntry, _comparerModel)
                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                            .WithMaybeFilter()
                            .CountFilteredDocs(_statisticUtilities)
                            .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService, indexName)
                            .RunIgnore(_actorSystem.Materializer());

                        _logger.LogInformation("finished processing pdf documents");
                        sw.Stop();
                        await _elasticSearchService.FlushIndexAsync(indexName);
                        await _elasticSearchService.RefreshIndexAsync(indexName);
                        jobStatistic.EndJob = DateTime.Now;
                        jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                        jobStatistic.EntireDocCount = _statisticUtilities.EntireDocumentsCount();
                        jobStatistic.ProcessingError =
                            _statisticUtilities.FailedDocumentsCount();
                        jobStatistic.IndexedDocCount =
                            _statisticUtilities.ChangedDocumentsCount();
                        _statisticUtilities.AddJobStatisticToDatabase(
                            jobStatistic);
                        _logger.LogInformation("index documents in {ElapsedMillis} ms",
                            sw.ElapsedMilliseconds);
                        _comparerModel.RemoveComparerFile();
                        await _comparerModel.WriteAllLinesAsync();
                        _jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "An error in processing pipeline occured");
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
        (@"[^a-zA-Zäöüß@.0-9]", " "),
        (Environment.NewLine, " "),
        ("[ ]{2,}", " ")
    );

    private static string Unescape(this string value) => Regex.Unescape(value);

    private static async Task<Option<MsgElasticDocument>> ProcessMsgDocument(this string fileName,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelMsg> statisticUtilities,
        ILogger logger)
    {
        try
        {
            (string Content, string Title, string Id, string Creator, IEnumerable<string>
                recipients)
                resultSet =
                    await Task.Run(() =>
                    {
                        using var msgReader = new Storage.Message(fileName);
                        // var content = msgReader.BodyText.IsNullOrWhiteSpace()
                        //     ? Either<string, string>.Left(msgReader.BodyText ?? "")
                        //     : Either<string, string>.Right(msgReader.BodyText ?? "");
                        var content = msgReader.BodyText ?? "";
                        var title = msgReader.Subject;
                        var id = msgReader.Id ?? Guid.NewGuid().ToString();
                        var creator = msgReader.Sender.Email;
                        var receiver =
                            msgReader.Recipients.Map(recipient => recipient.Email);
                        msgReader.Dispose();
                        return (content, title, id, creator, receiver);
                    });


            var uriPath = fileName
                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var fileNameHash = await StaticHelpers.CreateHashString(TypedHashedInputString.New(fileName));

            var cleanContent = resultSet
                .Content;
                // .ReplaceSpecialStrings(ToReplaced)
                // .Unescape();


            var searchAsYouTypeContent = cleanContent
                .ToLower()
                .Split(" ")
                .Distinct()
                .Filter(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                .Filter(d => d.Length() > 2);
            var completionField = new CompletionField {Input = searchAsYouTypeContent};

            var listElementsToHash = List(cleanContent, resultSet.Creator, resultSet.Title, "pdf");
            var contentHash =
                (await StaticHelpers.CreateHashString(TypedHashedInputString.New(listElementsToHash.Concat()))).Value;

            var elasticDoc = new MsgElasticDocument
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
            return Some(elasticDoc);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occured");
            statisticUtilities.AddToFailedDocuments();
            return Option<MsgElasticDocument>.None;
        }
    }
}