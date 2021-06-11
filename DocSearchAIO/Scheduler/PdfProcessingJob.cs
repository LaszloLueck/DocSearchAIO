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
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using LiteDB;
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
        private readonly StatisticUtilities _statisticUtilities;
        private readonly Comparers<PdfElasticDocument> _comparers;
        
public PdfProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
            IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<PdfProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = new StatisticUtilities(loggerFactory, liteDatabase);
            _comparers = new Comparers<PdfElasticDocument>(liteDatabase);
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
                            var indexName = _schedulerUtilities.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

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
                                        var jobStatistic = new ProcessingJobStatistic
                                        {
                                            Id = Guid.NewGuid().ToString(), StartJob = DateTime.Now
                                        };
                                        var sw = Stopwatch.StartNew();
                                        var runnable = Source
                                            .From(Directory.GetFiles(scanPath, schedulerEntry.FileExtension,
                                                SearchOption.AllDirectories))
                                            .Where(file =>
                                                _schedulerUtilities.UseExcludeFileFilter(schedulerEntry.ExcludeFilter,
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
                                                    await _elasticSearchService.BulkWriteDocumentsAsync(processingInfo,
                                                        indexName))
                                            .RunWith(Sink.Ignore<bool>(), materializer);
                                        await Task.WhenAll(runnable);

                                        _logger.LogInformation("finished processing pdf documents");
                                        sw.Stop();
                                        jobStatistic.EndJob = DateTime.Now;
                                        jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                                        jobStatistic.EntireDocCount = _statisticUtilities.GetEntireDocumentsCount();
                                        jobStatistic.ProcessingError = _statisticUtilities.GetFailedDocumentsCount();
                                        jobStatistic.IndexedDocCount = _statisticUtilities.GetChangedDocumentsCount();
                                        _statisticUtilities.AddJobStatisticToDatabase<PdfElasticDocument>(jobStatistic);
                                        _logger.LogInformation("index documents in {ElapsedMillis} ms",
                                            sw.ElapsedMilliseconds);
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
                    var md5 = MD5.Create();
                    var pdfReader = new PdfReader(fileName);
                    using var document = new PdfDocument(pdfReader);
                    var info = document.GetDocumentInfo();
                    var pdfPages = new ConcurrentBag<PdfPage>();

                    for (var i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        var pdfPage = document.GetPage(i);
                        pdfPages.Add(
                            new PdfPage(PdfTextExtractor.GetTextFromPage(pdfPage, new SimpleTextExtractionStrategy())));
                    }

                    var uriPath = fileName
                        .Replace(configuration.ScanPath, _cfg.UriReplacement)
                        .Replace(@"\", "/");

                    var elasticDoc = new PdfElasticDocument
                    {
                        OriginalFilePath = fileName,
                        PageCount = pdfPages.Count,
                        Creator = info.GetCreator(),
                        Keywords = info.GetKeywords() == null || info.GetKeywords().Length == 0
                            ? Array.Empty<string>()
                            : info.GetKeywords().Split(" "),
                        Subject = info.GetSubject(),
                        Title = info.GetTitle(),
                        ProcessTime = DateTime.Now,

                        Id = Convert.ToBase64String(
                            md5.ComputeHash(Encoding.UTF8.GetBytes(fileName))),
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
                    elasticDoc.ContentHash = await _schedulerUtilities.CreateHashString(listElementsToHash);

                    return Maybe<PdfElasticDocument>.From(elasticDoc);
                }
                catch (Exception exception)
                {
                    _logger.LogInformation(exception, "An error occured");
                    _statisticUtilities.AddToFailedDocuments();
                    return Maybe<PdfElasticDocument>.None;
                }
            });
        }
    }

    internal class PdfPage
    {
        public readonly string PageText;

        public PdfPage(string pageText)
        {
            PageText = pageText;
        }
    }
}