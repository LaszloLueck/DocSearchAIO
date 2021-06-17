using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util.Internal;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
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
        private readonly StatisticUtilities<PdfElasticDocument> _statisticUtilities;
        private readonly ComparersBase<PdfElasticDocument> _comparers;
        private readonly JobStateMemoryCache<PdfElasticDocument> _jobStateMemoryCache;

        public PdfProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
            IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<PdfProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.PdfStatisticUtility(loggerFactory);
            _comparers = new ComparersBase<PdfElasticDocument>(loggerFactory, _cfg);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetPdfJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing[nameof(PdfElasticDocument)];
            await Task.Run(() =>
            {
                schedulerEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                schedulerEntry.TriggerName,
                                _cfg.GroupName);
                            _logger.LogWarning(
                                "skip Processing of PDF documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName =
                                _schedulerUtilities.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtilities.CheckAndCreateElasticIndex<PdfElasticDocument>(indexName);
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
                                            var runnable = Source
                                                .From(Directory.GetFiles(scanPath, schedulerEntry.FileExtension,
                                                    SearchOption.AllDirectories))
                                                .Where(file =>
                                                    _schedulerUtilities.UseExcludeFileFilter(
                                                        schedulerEntry.ExcludeFilter,
                                                        file))
                                                .CountEntireDocs(_statisticUtilities)
                                                .SelectAsync(schedulerEntry.Parallelism,
                                                    file => ProcessPdfDocument(file, _cfg))
                                                .SelectAsync(schedulerEntry.Parallelism,
                                                    elementOpt => _comparers.FilterExistingUnchanged(elementOpt))
                                                .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                                .WithMaybeFilter()
                                                .CountFilteredDocs(_statisticUtilities)
                                                .SelectAsync(schedulerEntry.Parallelism,
                                                    async processingInfo =>
                                                        await _elasticSearchService.BulkWriteDocumentsAsync(
                                                            processingInfo,
                                                            indexName))
                                                .RunWith(Sink.Ignore<bool>(), materializer);
                                            await Task.WhenAll(runnable);

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
                                            _comparers.RemoveComparerFile();
                                            await _comparers.WriteAllLinesAsync();
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

        private async Task<Maybe<PdfElasticDocument>> ProcessPdfDocument(string fileName,
            ConfigurationObject configuration)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var pdfReader = new PdfReader(fileName);
                    using var document = new PdfDocument(pdfReader);
                    var info = document.GetDocumentInfo();
                    var pdfPages = new ConcurrentBag<PdfPageObject>();

                    for (var i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        var pdfPage = document.GetPage(i);
                        pdfPages.Add(new PdfPageObject(
                            PdfTextExtractor.GetTextFromPage(pdfPage, new SimpleTextExtractionStrategy())));
                    }
                    
                    toProcess
                        .ForEach(page => pdfPages.Add(new PdfPageObject(PdfTextExtractor.GetTextFromPage(page, new SimpleTextExtractionStrategy()))));


                    var creator = info.GetCreator();
                    var keywords = info.GetKeywords() == null || info.GetKeywords().Length == 0
                        ? Array.Empty<string>()
                        : info.GetKeywords().Split(" ");
                    var subject = info.GetSubject();
                    var title = info.GetTitle();
                    
                    document.Close();
                    
                    var uriPath = fileName
                        .Replace(configuration.ScanPath, _cfg.UriReplacement)
                        .Replace(@"\", "/");

                    var fileNameHash = await StaticHelpers.CreateMd5HashString(fileName);
                    
                    var elasticDoc = new PdfElasticDocument
                    {
                        OriginalFilePath = fileName,
                        PageCount = pdfPages.Count,
                        Creator = creator,
                        Keywords = keywords,
                        Subject = subject,
                        Title = title,
                        ProcessTime = DateTime.Now,
                        Id = fileNameHash,
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
                    elasticDoc.ContentHash = await StaticHelpers.CreateMd5HashString(listElementsToHash.JoinString(""));

                    return Maybe<PdfElasticDocument>.From(elasticDoc);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "An error occured");
                    _statisticUtilities.AddToFailedDocuments();
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