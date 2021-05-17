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
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class PdfProcessingJob : IJob
    {
        private ILogger _logger;
        private static readonly SHA256 Sha256 = SHA256.Create();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationObject) context.JobDetail.JobDataMap["configuration"];
            var actorSystem = (ActorSystem) context.JobDetail.JobDataMap["actorSystem"];
            var materializer = actorSystem.Materializer();
            var schedulerEntry = configuration.Processing["pdf"];
            var schedulerUtils = new SchedulerUtils();

            await Task.Run(async () =>
            {
                _logger = LoggingFactoryBuilder.Build<PdfProcessingJob>();
                _logger.LogInformation("Start Job");
                var uriList = configuration.ElasticEndpoints.Select(uri => new Uri(uri));
                var elasticClient = new ElasticConnection(uriList);
                var indexName = configuration.IndexName + "-" + schedulerEntry.IndexSuffix;

                if (!await elasticClient.IndexExistsAsync(indexName))
                {
                    _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                    await elasticClient.CreateIndexAsync<ElasticDocument>(indexName);
                    await elasticClient.RefreshIndexAsync(indexName);
                    await elasticClient.FlushIndexAsync(indexName);
                }

                var compareDirectory = await schedulerUtils.CreateComparerDirectoryIfNotExists(schedulerEntry);
                var comparerBag = FillComparerBag(compareDirectory);
                _logger.LogInformation("start crunching and indexing some pdf-files");
                if (!Directory.Exists(configuration.ScanPath))
                {
                    _logger.LogWarning($"directory to scan <{configuration.ScanPath}> does not exists. skip working.");
                }
                else
                {
                    var sw = Stopwatch.StartNew();


                    var procCount = 0;
                    var source = Source
                        .From(Directory.GetFiles(configuration.ScanPath, schedulerEntry.FileExtension,
                            SearchOption.AllDirectories))
                        .Where(file => UseExcludeFileFilter(schedulerEntry, file))
                        .SelectAsync(10, file => ProcessPdfDocument(file, configuration))
                        .SelectAsync(1, elementOpt =>
                        {
                            procCount += 1;
                            if (procCount % 100 == 0)
                                _logger.LogInformation($"processing {procCount} documents.");
                            return FilterExistingUnchanged(elementOpt, comparerBag);
                        })
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .Select(d => d.Values())
                        .SelectAsync(6,
                            async elasticDocs => await elasticClient.BulkWriteDocumentsAsync(elasticDocs, indexName));
                    var runnable = source.Limit(200).RunWith(Sink.Seq<bool>(), materializer);
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

        private static async Task<Option<PdfElasticDocument>> FilterExistingUnchanged(Option<PdfElasticDocument> document,
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
                        pdfPages.Add(new PdfPage(i, PdfTextExtractor.GetTextFromPage(pdfPage, new SimpleTextExtractionStrategy())));
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

    internal class PdfDocumentContainer
    {
        public readonly string OriginalFileName;
        public readonly List<PdfPage> PdfPages;
        public readonly PdfDocumentInfo PdfDocumentInfo;

        public PdfDocumentContainer(string originalFileName, List<PdfPage> pdfPages, PdfDocumentInfo pdfDocumentInfo)
        {
            OriginalFileName = originalFileName;
            PdfPages = pdfPages;
            PdfDocumentInfo = pdfDocumentInfo;
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