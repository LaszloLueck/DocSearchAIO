using System.Collections.Concurrent;
using System.Diagnostics;
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
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;
using Nest;
using Quartz;
using Array = System.Array;

namespace DocSearchAIO.Scheduler.PdfJobs;

[DisallowConcurrentExecution]
public class PdfProcessingJob : IJob
{
    private readonly IConfigurationUpdater _cfgConfigurationUpdater;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public PdfProcessingJob(IConfigurationUpdater configurationUpdater, ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities,
        IElasticUtilities elasticUtilities)
    {
        _memoryCache = memoryCache;
        _cfgConfigurationUpdater = configurationUpdater;
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
    }

    [Time]
    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<PdfProcessingJob>();
        var cfg = await _cfgConfigurationUpdater.ReadConfigurationAsync();
        var configEntry = cfg.Processing[nameof(PdfElasticDocument)];

        
        var statisticUtilities = StatisticUtilitiesProxy.PdfStatisticUtility(
            TypedDirectoryPathString.New(cfg.StatisticsDirectory),
            new StatisticModelPdf().StatisticFileName);
        var comparerModel = new ComparerModelPdf(cfg.ComparerDirectory);
        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetPdfJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();

        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelPdfCleanup());
        if (cacheEntryOpt.IsSome &&
            (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }

        if (!configEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                configEntry.TriggerName,
                cfg.SchedulerGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip cleanup of PDF documents because the scheduler is inactive per config");
        }
        else
        {
            logger.LogInformation("start job");
            var indexName =
                _elasticUtilities.CreateIndexName(cfg.IndexName, configEntry.IndexSuffix);

            await _elasticUtilities.CheckAndCreateElasticIndex<PdfElasticDocument>(indexName);
            logger.LogInformation("start crunching and indexing some pdf-files");
            if (!Directory.Exists(cfg.ScanPath))
            {
                logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists, skip working", cfg.ScanPath);
            }
            else
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
                    await TypedFilePathString.New(cfg.ScanPath)
                        .CreateSource(configEntry.FileExtension)
                        .UseExcludeFileFilter(configEntry.ExcludeFilter)
                        .CountEntireDocs(statisticUtilities)
                        .ProcessPdfDocumentAsync(configEntry, cfg, statisticUtilities, logger)
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
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error in processing pipeline occured");
                }
            }
        }
    }
}

internal static class PdfProcessingHelper
{
    public static Source<Option<PdfElasticDocument>, NotUsed> ProcessPdfDocumentAsync(
        this Source<string, NotUsed> source,
        SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelPdf> statisticUtilities, ILogger logger)
    {
        return source.SelectAsync(schedulerEntry.Parallelism,
            f => ProcessPdfDocument(f, configurationObject, statisticUtilities, logger));
    }

    private static async Task<Option<PdfElasticDocument>> ProcessPdfDocument(this string fileName,
        ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelPdf> statisticUtilities, ILogger logger)
    {
        try
        {


            (ConcurrentBag<PdfPageObject> PdfPages, string Creator, string[] Keywords, string Subject, string Title)
                pdfObject = await Task.Run(() =>
                {
                    var pdfReader = new PdfReader(fileName);
                    using var document = new PdfDocument(pdfReader);
                    var info = document.GetDocumentInfo();
                    var tmpPages = new ConcurrentBag<PdfPageObject>();
                    for (var i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        var pdfPage = document.GetPage(i);
                        tmpPages.Add(new PdfPageObject(
                            PdfTextExtractor.GetTextFromPage(pdfPage, new SimpleTextExtractionStrategy())));
                    }

                    var creator = info.GetCreator();
                    var keywords = info.GetKeywords() == null || info.GetKeywords().Length == 0
                        ? Array.Empty<string>()
                        : info.GetKeywords().Split(" ");
                    var subject = info.GetSubject();
                    var title = info.GetTitle();
                    document.Close();

                    return (tmpPages, creator, keywords, subject, title);
                });
            var uriPath = fileName
                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var fileNameHash =
                await StaticHelpers.CreateHashString(TypedHashedInputString.New(fileName));

            var elasticDoc = new PdfElasticDocument
            {
                OriginalFilePath = fileName,
                PageCount = pdfObject.PdfPages.Count,
                Creator = pdfObject.Creator,
                Keywords = pdfObject.Keywords,
                Subject = pdfObject.Subject,
                Title = pdfObject.Title,
                ProcessTime = DateTime.Now,
                Id = fileNameHash.Value,
                UriFilePath = uriPath,
                ContentType = "pdf"
            };

            var contentString = await Task.Run(() => pdfObject.PdfPages.Map(p => p.PageText).Join(" "));
            var suggestedText = await Task.Run(() => Regex.Replace(contentString, "[^a-zA-Zäöüß]", " "));
            var searchAsYouTypeContent = suggestedText
                    .ToLower()
                    .Split(" ")
                    .Distinct()
                    .Filter(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                    .Filter(d => d.Length > 2);
            var completionField = new CompletionField { Input = searchAsYouTypeContent };
            elasticDoc.CompletionContent = completionField;

            var listElementsToHash = new List<string>
            {
                contentString, elasticDoc.Creator, elasticDoc.Keywords.Concat(),
                elasticDoc.Title, elasticDoc.Subject, elasticDoc.ContentType
            };
            elasticDoc.Content = contentString;
            elasticDoc.ContentHash =
                (await StaticHelpers.CreateHashString(TypedHashedInputString.New(listElementsToHash.Concat())))
                .Value;

            return elasticDoc;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occured");
            statisticUtilities.AddToFailedDocuments();
            return Option<PdfElasticDocument>.None;
        }
    }
}

internal class PdfPageObject
{
    public readonly string PageText;

    public PdfPageObject(string pageText)
    {
        PageText = pageText;
    }
}