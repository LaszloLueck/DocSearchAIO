using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Caching.Memory;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler.PdfJobs;

[DisallowConcurrentExecution]
public class PdfProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly SchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelPdf> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelPdf> _jobStateMemoryCache;
    private readonly ElasticUtilities _elasticUtilities;

    public PdfProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
        IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
    {
        _logger = loggerFactory.CreateLogger<PdfProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = new SchedulerUtilities(loggerFactory);
        _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
        _statisticUtilities = StatisticUtilitiesProxy.PdfStatisticUtility(loggerFactory,
            new TypedDirectoryPathString(_cfg.StatisticsDirectory),
            new StatisticModelPdf().StatisticFileName);
        _comparerModel = new ComparerModelPdf(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache = JobStateMemoryCacheProxy.GetPdfJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var configEntry = _cfg.Processing[nameof(PdfElasticDocument)];

        await Task.Run(async () =>
        {
            var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelPdfCleanup());
            if (!cacheEntryOpt.HasNoValue &&
                (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
            {
                _logger.LogInformation(
                    "cannot execute scanning and processing documents, opponent job cleanup running");
                return;
            }

            if (!configEntry.Active)
            {
                await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                    configEntry.TriggerName,
                    _cfg.SchedulerGroupName, TriggerState.Paused);
                _logger.LogWarning(
                    "skip cleanup of PDF documents because the scheduler is inactive per config");
            }
            else
            {
                _logger.LogInformation("start job");
                var indexName =
                    _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);

                await _elasticUtilities.CheckAndCreateElasticIndex<PdfElasticDocument>(indexName);
                _logger.LogInformation("start crunching and indexing some pdf-files");
                if (!Directory.Exists(_cfg.ScanPath))
                {
                    _logger.LogWarning(
                        "directory to scan <{ScanPath}> does not exists, skip working", _cfg.ScanPath);
                }
                else
                {
                    try
                    {
                        _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                        var jobStatistic = new ProcessingJobStatistic
                        {
                            Id = Guid.NewGuid().ToString(), StartJob = DateTime.Now
                        };
                        var sw = Stopwatch.StartNew();
                        await new TypedFilePathString(_cfg.ScanPath)
                            .CreateSource(configEntry.FileExtension)
                            .UseExcludeFileFilter(configEntry.ExcludeFilter)
                            .CountEntireDocs(_statisticUtilities)
                            .ProcessPdfDocumentAsync(configEntry, _cfg, _statisticUtilities, _logger)
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error in processing pipeline occured");
                    }
                }
            }
        });
    }
}

internal static class PdfProcessingHelper
{
    public static Source<Maybe<PdfElasticDocument>, NotUsed> ProcessPdfDocumentAsync(
        this Source<string, NotUsed> source,
        SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelPdf> statisticUtilities, ILogger logger)
    {
        return source.SelectAsync(schedulerEntry.Parallelism,
            f => ProcessPdfDocument(f, configurationObject, statisticUtilities, logger));
    }

    private static async Task<Maybe<PdfElasticDocument>> ProcessPdfDocument(this string fileName,
        ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelPdf> statisticUtilities, ILogger logger)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var pdfReader = new PdfReader(fileName);
                using var document = new PdfDocument(pdfReader);
                var info = document.GetDocumentInfo();
                var pdfPages = new ConcurrentBag<PdfPageObject>();

                var toProcess = new ConcurrentBag<PdfPage>();


                for (var i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var pdfPage = document.GetPage(i);
                    toProcess.Add(pdfPage);
                }

                toProcess
                    .ForEach(page =>
                        pdfPages.Add(new PdfPageObject(
                            PdfTextExtractor.GetTextFromPage(page, new SimpleTextExtractionStrategy()))));

                var creator = info.GetCreator();
                var keywords = info.GetKeywords() == null || info.GetKeywords().Length == 0
                    ? Array.Empty<string>()
                    : info.GetKeywords().Split(" ");
                var subject = info.GetSubject();
                var title = info.GetTitle();

                document.Close();

                var uriPath = fileName
                    .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                    .Replace(@"\", "/");

                var fileNameHash =
                    await StaticHelpers.CreateHashString(new TypedHashedInputString(fileName));

                var elasticDoc = new PdfElasticDocument
                {
                    OriginalFilePath = fileName,
                    PageCount = pdfPages.Count,
                    Creator = creator,
                    Keywords = keywords,
                    Subject = subject,
                    Title = title,
                    ProcessTime = DateTime.Now,
                    Id = fileNameHash.Value,
                    UriFilePath = uriPath,
                    ContentType = "pdf"
                };

                var contentString = pdfPages.Select(p => p.PageText).Join(" ");
                var suggestedText = Regex.Replace(contentString, "[^a-zA-Zäöüß]", " ");
                var searchAsYouTypeContent = suggestedText
                    .ToLower()
                    .Split(" ")
                    .Distinct()
                    .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                    .Where(d => d.Length > 2);
                var completionField = new CompletionField { Input = searchAsYouTypeContent };
                elasticDoc.CompletionContent = completionField;

                var listElementsToHash = new List<string>
                {
                    contentString, elasticDoc.Creator, elasticDoc.Keywords.Concat(),
                    elasticDoc.Title, elasticDoc.Subject, elasticDoc.ContentType
                };
                elasticDoc.Content = contentString;
                elasticDoc.ContentHash =
                    (await StaticHelpers.CreateHashString(new TypedHashedInputString(listElementsToHash.Concat()))).Value;

                return Maybe<PdfElasticDocument>.From(elasticDoc);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occured");
                statisticUtilities.AddToFailedDocuments();
                return Maybe<PdfElasticDocument>.None;
            }
        });
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