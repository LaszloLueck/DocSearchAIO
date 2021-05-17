using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
using Optional.Collections;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class OfficeWordProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject cfg;
        private readonly ActorSystem _actorSystem;
        private readonly ElasticSearchService _elasticSearchService;
        private readonly SchedulerUtils _schedulerUtils;

        public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
            cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = new ElasticSearchService(loggerFactory, elasticClient);
            _schedulerUtils = new SchedulerUtils(loggerFactory);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                var schedulerEntry = cfg.Processing["word"];
                if (schedulerEntry.Active)
                {
                    var materializer = _actorSystem.Materializer();
                    _logger.LogInformation("Start Job");
                    var indexName = cfg.IndexName + "-" + schedulerEntry.IndexSuffix;

                    if (!await _elasticSearchService.IndexExistsAsync(indexName))
                    {
                        _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                        await _elasticSearchService.CreateIndexAsync<ElasticDocument>(indexName);
                        await _elasticSearchService.RefreshIndexAsync(indexName);
                        await _elasticSearchService.FlushIndexAsync(indexName);
                    }

                    var compareDirectory = await _schedulerUtils.CreateComparerDirectoryIfNotExists(schedulerEntry);

                    var comparerBag = FillConmparerBag(compareDirectory);

                    _logger.LogInformation("start crunching and indexing some word-documents");
                    if (!Directory.Exists(cfg.ScanPath))
                    {
                        _logger.LogWarning($"directory to scan <{cfg.ScanPath}> does not exists. skip working.");
                    }
                    else
                    {
                        var sw = Stopwatch.StartNew();
                        var source = Source
                            .From(Directory.GetFiles(cfg.ScanPath, schedulerEntry.FileExtension,
                                SearchOption.AllDirectories))
                            .Where(file => !file.Contains(schedulerEntry.ExcludeFilter))
                            .SelectAsync(schedulerEntry.Parallelism, fileName => ProcessWordDocument(fileName, cfg))
                            .SelectAsync(parallelism: schedulerEntry.Parallelism,
                                elementOpt => FilterExistingUnchanged(elementOpt, comparerBag))
                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                            .Select(d => d.Values())
                            .SelectAsync(schedulerEntry.Parallelism,
                                async processingInfo =>
                                    await _elasticSearchService.BulkWriteDocumentsAsync(@processingInfo, indexName));

                        var runnable = source.RunWith(Sink.Seq<bool>(), materializer);
                        await Task.WhenAll(runnable);

                        _logger.LogInformation("finished processing word-documents.");
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
                        "Skip Processing of Word documents because the scheduler is inactive per config");
                }
            });
        }

        private static ConcurrentDictionary<string, string> FillConmparerBag(string fileName)
        {
            var str = File.ReadLines(fileName);
            var cnv = str.Select(str =>
            {
                var spl = str.Split(";");
                return new KeyValuePair<string, string>(spl[0], spl[1]);
            });

            return new ConcurrentDictionary<string, string>(cnv);
        }


        private async Task<Option<ElasticDocument>> FilterExistingUnchanged(Option<ElasticDocument> document,
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

                    if (currentHash == value) return Option.None<ElasticDocument>();
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }
                });
                return opt;
            });
        }


        private async Task<Option<ElasticDocument>> ProcessWordDocument(string currentFile,
            ConfigurationObject configurationObject)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var wd = WordprocessingDocument.Open(currentFile, false);
                    var sha256 = SHA256.Create();
                    var fInfo = wd.PackageProperties;

                    var category = fInfo.Category.SomeNotNull().ValueOr("");
                    var created = fInfo.Created ?? new DateTime(1970, 1, 1);
                    var creator = fInfo.Creator.SomeNotNull().ValueOr("");
                    var description = fInfo.Description.SomeNotNull().ValueOr("");
                    var identifier = fInfo.Identifier.SomeNotNull().ValueOr("");
                    var keywords = fInfo.Keywords.SomeNotNull().ValueOr("");
                    var language = fInfo.Language.SomeNotNull().ValueOr("");
                    var modified = fInfo.Modified ?? new DateTime(1970, 1, 1);
                    var revision = fInfo.Revision.SomeNotNull().ValueOr("");
                    var subject = fInfo.Subject.SomeNotNull().ValueOr("");
                    var title = fInfo.Title.SomeNotNull().ValueOr("");
                    var version = fInfo.Version.SomeNotNull().ValueOr("");
                    var contentStatus = fInfo.ContentStatus.SomeNotNull().ValueOr("");
                    const string contentType = "docx";
                    var lastPrinted = fInfo.LastPrinted ?? new DateTime(1970, 1, 1);
                    var lastModifiedBy = fInfo.LastModifiedBy.SomeNotNull().ValueOr("");
                    var uriPath = currentFile
                        .Replace(configurationObject.ScanPath, @"https://risprepository:8800/svns/PNR/extern")
                        .Replace(@"\", "/");

                    var idAsByte = sha256.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                    var id = Convert.ToBase64String(idAsByte);

                    var commentArray = wd.MainDocumentPart.WordprocessingCommentsPart.SomeNotNull().Match(
                        some: comments =>
                        {
                            return comments.Comments.Select(comment =>
                            {
                                var d = (Comment) comment;
                                var retValue = new OfficeDocumentComment();
                                var dat = d.Date != null
                                    ? d.Date.Value.SomeNotNull().ValueOr(new DateTime(1970, 1, 1))
                                    : new DateTime(1970, 1, 1);

                                retValue.Author = d.Author.Value;
                                retValue.Comment = d.InnerText;
                                retValue.Date = dat;
                                retValue.Id = d.Id.Value;
                                retValue.Initials = d.Initials.Value;
                                return retValue;
                            }).ToArray();
                        },
                        none: Array.Empty<OfficeDocumentComment>
                    );

                    var elements = wd
                        .MainDocumentPart
                        .Document
                        .Body
                        .ChildElements;

                    var contentString = GetChildElements(elements);
                    var toReplaced = new List<(string, string)>();

                    ReplaceSpecialStringsTailR(ref contentString, toReplaced);

                    var keywordsList = keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

                    var commentsString = commentArray.Select(l => l.Comment.Split(" ")).Distinct().ToList();

                    var listElementsToHash = new List<string>()
                    {
                        category, created.ToString(CultureInfo.CurrentCulture), contentString, creator, description,
                        identifier,
                        string.Join("", keywords), language, modified.ToString(CultureInfo.CurrentCulture), revision,
                        subject, title, version,
                        contentStatus, contentType, lastPrinted.ToString(CultureInfo.CurrentCulture), lastModifiedBy
                    };

                    var res = listElementsToHash.Concat(commentsString.SelectMany(k => k).Distinct());

                    var contentHashString = CreateHashString(res);

                    var commString = string.Join(" ", commentArray.Select(d => d.Comment));
                    var suggestedText = Regex.Replace(contentString + " " + commString, "[^a-zA-ZäöüßÄÖÜ]", " ");


                    var searchAsYouTypeContent = suggestedText
                        .ToLower()
                        .Split(" ")
                        .Distinct()
                        .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                        .Where(d => d.Length > 2);

                    var completionField = new CompletionField {Input = searchAsYouTypeContent};

                    var returnValue = new ElasticDocument()
                    {
                        Category = category, CompletionContent = completionField, Content = contentString,
                        ContentHash = contentHashString, ContentStatus = contentStatus, ContentType = contentType,
                        Created = created, Creator = creator, Description = description, Id = id,
                        Identifier = identifier, Keywords = keywordsList, Language = language, Modified = modified,
                        Revision = revision, Subject = subject, Title = title, Version = version,
                        LastPrinted = lastPrinted, ProcessTime = DateTime.Now, LastModifiedBy = lastModifiedBy,
                        OriginalFilePath = currentFile, UriFilePath = uriPath, Comments = commentArray
                    };

                    return Option.Some(returnValue);
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error while creating a indexing object");
                return await Task.Run(Option.None<ElasticDocument>);
            }
        }

        private string CreateHashString(IEnumerable<string> elements)
        {
            var contentString = string.Join("", elements);
            var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(contentString));
            return Convert.ToBase64String(hash);
        }

        private string GetChildElements(IEnumerable<OpenXmlElement> list)
        {
            var elementString = list.Select(CheckElementAndReturnString);
            return string.Join("", elementString);
        }

        private string CheckElementAndReturnString(OpenXmlElement element)
        {
            return element.LocalName switch
            {
                "t" when !element.ChildElements.Any() => element.InnerText,
                "p" => GetChildElements(element.ChildElements) + " ",
                "br" => " ",
                _ => GetChildElements(element.ChildElements)
            };
        }

        private void ReplaceSpecialStringsTailR(ref string input, IList<(string, string)> replaceList)
        {
            if (!replaceList.Any())
                return;

            input = Regex.Replace(input, replaceList[0].Item1, replaceList[0].Item2);
            replaceList.RemoveAt(0);
            ReplaceSpecialStringsTailR(ref input, replaceList);
        }
    }
}