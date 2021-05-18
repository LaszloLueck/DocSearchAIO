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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
using Optional.Collections;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class PdfProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private static readonly SHA256 Sha256 = SHA256.Create();
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly ElasticSearchService _elasticSearchService;
        private readonly SchedulerUtils _schedulerUtils;

        public PdfProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
            IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<PdfProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = new ElasticSearchService(loggerFactory, elasticClient);
            _schedulerUtils = new SchedulerUtils(loggerFactory);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                var schedulerEntry = _cfg.Processing["pdf"];
                if (schedulerEntry.Active)
                {
                    var materializer = _actorSystem.Materializer();
                    _logger.LogInformation("Start Job");
                    var indexName = _cfg.IndexName + "-" + schedulerEntry.IndexSuffix;

                    if (!await _elasticSearchService.IndexExistsAsync(indexName))
                    {
                        _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                        await _elasticSearchService.CreateIndexAsync<PdfElasticDocument>(indexName);
                        await _elasticSearchService.RefreshIndexAsync(indexName);
                        await _elasticSearchService.FlushIndexAsync(indexName);
                    }

                    var compareDirectory = await _schedulerUtils.CreateComparerDirectoryIfNotExists(schedulerEntry);
                    var comparerBag = FillComparerBag(compareDirectory);
                    _logger.LogInformation("start crunching and indexing some pdf-files");
                    if (!Directory.Exists(_cfg.ScanPath))
                    {
                        _logger.LogWarning(
                            $"directory to scan <{_cfg.ScanPath}> does not exists. skip working.");
                    }
                    else
                    {
                        var sw = Stopwatch.StartNew();
                        var source = Source
                            .From(Directory.GetFiles(_cfg.ScanPath, schedulerEntry.FileExtension,
                                SearchOption.AllDirectories))
                            .Where(file => UseExcludeFileFilter(schedulerEntry, file))
                            .SelectAsync(schedulerEntry.Parallelism, file => ProcessPdfDocument(file, _cfg))
                            .SelectAsync(schedulerEntry.Parallelism,
                                elementOpt => FilterExistingUnchanged(elementOpt, comparerBag))
                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                            .Select(d => d.Values())
                            .SelectAsync(schedulerEntry.Parallelism,
                                async elasticDocs =>
                                    await _elasticSearchService.BulkWriteDocumentsAsync(elasticDocs, indexName));
                        var runnable = source.RunWith(Sink.Seq<bool>(), materializer);
                        await Task.WhenAll(runnable);

                        _logger.LogInformation("finished processing pdf-documents.");
                        _logger.LogInformation($"delete comparer file in <{compareDirectory}>");
                        File.Delete(compareDirectory);
                        _logger.LogInformation($"write new comparer file in {compareDirectory}");
                        await File.WriteAllLinesAsync(compareDirectory,
                            comparerBag.Select(tpl => tpl.Key + ";" + tpl.Value));

                        sw.Stop();
                        _logger.LogInformation($"index documents in {sw.ElapsedMilliseconds} ms");
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Skip Processing of PDF documents because the scheduler is inactive per config");
                }
            });
        }

        private static bool UseExcludeFileFilter(SchedulerEntry schedulerEntry, string fileName)
        {
            if (schedulerEntry.ExcludeFilter == "")
                return true;

            return !fileName.Contains(schedulerEntry.ExcludeFilter);
        }

        private static ConcurrentDictionary<string, string> FillComparerBag(string fileName)
        {
            var str = File.ReadLines(fileName);
            var cnv = str.Select(str =>
            {
                var spl = str.Split(";");
                return new KeyValuePair<string, string>(spl[0], spl[1]);
            });

            return new ConcurrentDictionary<string, string>(cnv);
        }

        private static async Task<Option<PdfElasticDocument>> FilterExistingUnchanged(
            Option<PdfElasticDocument> document,
            ConcurrentDictionary<string, string> comparerBag)
        {
            return await Task.Run(() =>
            {
                var opt = document.FlatMap(doc =>
                {
                    var currentHash = doc.ContentHash;

                    if (!comparerBag.TryGetValue(doc.Id, out var value))
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }

                    if (currentHash == value) return Option.None<PdfElasticDocument>();
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }
                });
                return opt;
            });
        }

        private async Task<Option<PdfElasticDocument>> ProcessPdfDocument(string fileName,
            ConfigurationObject configuration)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var pdfReader = new PdfReader(fileName);
                    var document = new PdfDocument(pdfReader);
                    var info = document.GetDocumentInfo();
                    var pdfPages = new ConcurrentBag<PdfPage>();

                    for (var i = 1; i <= document.GetNumberOfPages(); i++)
                    {
                        var pdfPage = document.GetPage(i);
                        pdfPages.Add(new PdfPage(i,
                            PdfTextExtractor.GetTextFromPage(pdfPage, new SimpleTextExtractionStrategy())));
                    }

                    var uriPath = fileName
                        .Replace(configuration.ScanPath, @"https://risprepository:8800/svns/PNR/extern")
                        .Replace(@"\", "/");

                    var elasticDoc = new PdfElasticDocument();
                    elasticDoc.OriginalFilePath = fileName;
                    elasticDoc.PageCount = pdfPages.Count;
                    elasticDoc.Creator = info.GetCreator();
                    elasticDoc.Keywords = info.GetKeywords() == null || info.GetKeywords().Length == 0
                        ? Array.Empty<string>()
                        : info.GetKeywords().Split(" ");
                    elasticDoc.Subject = info.GetSubject();
                    elasticDoc.Title = info.GetTitle();
                    elasticDoc.ProcessTime = DateTime.Now;
                    elasticDoc.Created = new DateTime(1970, 1, 1);
                    elasticDoc.Modified = new DateTime(1970, 1, 1);
                    elasticDoc.LastPrinted = new DateTime(1970, 1, 1);
                    elasticDoc.Id =
                        Convert.ToBase64String(
                            Sha256.ComputeHash(Encoding.UTF8.GetBytes(fileName)));
                    elasticDoc.UriFilePath = uriPath;
                    elasticDoc.ContentType = "pdf";

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
                    elasticDoc.ContentHash = CreateHashString(listElementsToHash);

                    return Option.Some(elasticDoc);
                }
                catch (Exception exception)
                {
                    _logger.LogInformation(exception, "An error occured");
                    return Option.None<PdfElasticDocument>();
                }
            });
        }

        private static string CreateHashString(IEnumerable<string> elements)
        {
            var contentString = string.Join("", elements);
            var hash = Sha256.ComputeHash(Encoding.UTF8.GetBytes(contentString));
            return Convert.ToBase64String(hash);
        }
    }

    internal class PdfPage
    {
        public readonly int PageNumber;
        public readonly string PageText;

        public PdfPage(int pageNumber, string pageText)
        {
            PageNumber = pageNumber;
            PageText = pageText;
        }
    }
}