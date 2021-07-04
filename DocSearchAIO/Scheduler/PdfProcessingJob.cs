using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler
{
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
            _statisticUtilities = StatisticUtilitiesProxy.PdfStatisticUtility(loggerFactory, new TypedDirectoryPathString(_cfg.StatisticsDirectory),
                new StatisticModelPdf().GetStatisticFileName);
            _comparerModel = new ComparerModelPdf(loggerFactory, _cfg.ComparerDirectory);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetPdfJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var configEntry = _cfg.Processing[nameof(PdfElasticDocument)];

            await Task.Run(() =>
            {
                var cacheEntryOpt = _jobStateMemoryCache.GetCacheEntry(new MemoryCacheModelPdfCleanup());
                if (!cacheEntryOpt.HasNoValue &&
                    (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                {
                    _logger.LogInformation("cannot execute scanning and processing documents, opponent job cleanup running");
                    return;
                }
                
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.SchedulerGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of PDF documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            _logger.LogInformation("start job");
                            var indexName =
                                _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);

                            await _elasticUtilities.CheckAndCreateElasticIndex<PdfElasticDocument>(indexName);
                            _logger.LogInformation("start crunching and indexing some pdf-files");
                            Directory
                                .Exists(_cfg.ScanPath)
                                .IfTrueFalse((_cfg.ScanPath, _cfg.ScanPath),
                                    scanPath =>
                                    {
                                        _logger.LogWarning(
                                            "directory to scan <{ScanPath}> does not exists, skip working", scanPath);
                                    },
                                    async scanPath =>
                                    {
                                        try
                                        {
                                            _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                                            var jobStatistic = new ProcessingJobStatistic
                                            {
                                                Id = Guid.NewGuid().ToString(), StartJob = DateTime.Now
                                            };
                                            var sw = Stopwatch.StartNew();
                                            await new TypedFilePathString(scanPath)
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
                                            jobStatistic.EntireDocCount = _statisticUtilities.GetEntireDocumentsCount();
                                            jobStatistic.ProcessingError =
                                                _statisticUtilities.GetFailedDocumentsCount();
                                            jobStatistic.IndexedDocCount =
                                                _statisticUtilities.GetChangedDocumentsCount();
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
                                    });
                        }
                    );
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

        private static async Task<Maybe<PdfElasticDocument>> ProcessPdfDocument(this string fileName, ConfigurationObject configurationObject,
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

                    var fileNameHash = await StaticHelpers.CreateMd5HashString(new TypedMd5InputString(fileName));

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

                    var contentString = string.Join(" ", pdfPages.Select(p => p.PageText));
                    var suggestedText = Regex.Replace(contentString, "[^a-zA-Zäöüß]", " ");
                    var searchAsYouTypeContent = suggestedText
                        .ToLower()
                        .Split(" ")
                        .Distinct()
                        .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                        .Where(d => d.Length > 2);
                    var completionField = new CompletionField {Input = searchAsYouTypeContent};
                    elasticDoc.CompletionContent = completionField;

                    var listElementsToHash = new List<string>
                    {
                        contentString, elasticDoc.Creator, string.Join("", elasticDoc.Keywords),
                        elasticDoc.Title, elasticDoc.Subject, elasticDoc.ContentType
                    };
                    elasticDoc.Content = contentString;
                    elasticDoc.ContentHash = (await StaticHelpers.CreateMd5HashString(new TypedMd5InputString(listElementsToHash.JoinString("")))).Value;

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
}