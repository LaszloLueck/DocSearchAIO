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
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
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
        private readonly SchedulerUtils _schedulerUtils;

        public PdfProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
            IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<PdfProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtils = new SchedulerUtils(loggerFactory, elasticSearchService, liteDatabase);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing["pdf"];
            await Task.Run(() =>
            {
                schedulerEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtils.SetTriggerStateByUserAction(context.Scheduler,
                                schedulerEntry.TriggerName,
                                _cfg.GroupName);
                            _logger.LogWarning(
                                "skip Processing of PDF documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName = _schedulerUtils.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtils.CheckAndCreateElasticIndex<PdfElasticDocument>(indexName);
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
                                        var sw = Stopwatch.StartNew();
                                        var runnable = Source
                                            .From(Directory.GetFiles(scanPath, schedulerEntry.FileExtension,
                                                SearchOption.AllDirectories))
                                            .Where(file =>
                                                _schedulerUtils.UseExcludeFileFilter(schedulerEntry.ExcludeFilter, file))
                                            .SelectAsync(schedulerEntry.Parallelism, file => ProcessPdfDocument(file, _cfg))
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                elementOpt => _schedulerUtils.FilterExistingUnchanged(elementOpt))
                                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                            .WithOptionFilter()
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                async elasticDocs =>
                                                    await _elasticSearchService.BulkWriteDocumentsAsync(elasticDocs, indexName))
                                            .RunWith(Sink.Ignore<bool>(), materializer);
                                        await Task.WhenAll(runnable);

                                        _logger.LogInformation("finished processing pdf documents");
                                        sw.Stop();
                                        _logger.LogInformation("index documents in {ElapsedMillis} ms", sw.ElapsedMilliseconds);
                                    });
                        }
                    );
            });
        }

        private async Task<Option<PdfElasticDocument>> ProcessPdfDocument(string fileName,
            ConfigurationObject configuration)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var md5 = MD5.Create();
                    var pdfReader = new PdfReader(fileName);
                    var document = new PdfDocument(pdfReader);
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
                        Created = new DateTime(1970, 1, 1),
                        Modified = new DateTime(1970, 1, 1),
                        LastPrinted = new DateTime(1970, 1, 1),
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

                    var listElementsToHash = new List<string>()
                    {
                        contentString, elasticDoc.Creator, string.Join("", elasticDoc.Keywords),
                        elasticDoc.Title, elasticDoc.Subject, elasticDoc.ContentType
                    };
                    elasticDoc.Content = contentString;
                    elasticDoc.ContentHash = await _schedulerUtils.CreateHashString(listElementsToHash);

                    return Option.Some(elasticDoc);
                }
                catch (Exception exception)
                {
                    _logger.LogInformation(exception, "An error occured");
                    return Option.None<PdfElasticDocument>();
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