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
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
using Optional.Collections;
using Quartz;
using Quartz.Impl;
using IScheduler = Quartz.IScheduler;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class OfficePowerpointProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerUtils _schedulerUtils;

        public OfficePowerpointProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<OfficePowerpointProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtils = new SchedulerUtils(loggerFactory);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing["powerpoint"];
            await Task.Run(async () =>
            {
                if (schedulerEntry.Active)
                {
                    var materializer = _actorSystem.Materializer();
                    _logger.LogInformation("Start Job");
                    var indexName = _cfg.IndexName + "-" + schedulerEntry.IndexSuffix;

                    if (!await _elasticSearchService.IndexExistsAsync(indexName))
                    {
                        _logger.LogInformation($"Index {indexName} does not exist, lets create them");
                        await _elasticSearchService.CreateIndexAsync<PowerpointElasticDocument>(indexName);
                        await _elasticSearchService.RefreshIndexAsync(indexName);
                        await _elasticSearchService.FlushIndexAsync(indexName);
                    }

                    var compareDirectory = await _schedulerUtils.CreateComparerDirectoryIfNotExists(schedulerEntry);

                    var comparerBag = FillComparerBag(compareDirectory);

                    _logger.LogInformation("start crunching and indexing some powerpoint-documents");
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
                            .Where(file => !file.Contains(schedulerEntry.ExcludeFilter))
                            .SelectAsync(10, fileName => ProcessPowerpointDocument(fileName, _cfg))
                            .SelectAsync(parallelism: 10,
                                elementOpt => FilterExistingUnchanged(elementOpt, comparerBag))
                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                            .Select(d => d.Values())
                            .SelectAsync(6,
                                async processingInfo =>
                                    await _elasticSearchService.BulkWriteDocumentsAsync(@processingInfo, indexName));

                        var runnable = source.Limit(200).RunWith(Sink.Seq<bool>(), materializer);
                        await Task.WhenAll(runnable);

                        _logger.LogInformation("finished processing powerpoint-documents.");
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
                    var currentTriggerState =
                        await context.Scheduler.GetTriggerState(new TriggerKey(schedulerEntry.TriggerName,
                            _cfg.GroupName));
                    if (currentTriggerState is TriggerState.Blocked or TriggerState.Normal)
                    {
                        _logger.LogWarning(
                            $"Set Trigger for {schedulerEntry.TriggerName} in scheduler {context.Scheduler.SchedulerName} to pause because of user settings!");
                        await context.Scheduler.PauseTrigger(new TriggerKey(schedulerEntry.TriggerName,
                            _cfg.GroupName));
                    }
                    _logger.LogWarning(
                        "Skip Processing of Powerpoint documents because the scheduler is inactive per config");
                }
            });
        }

        private ConcurrentDictionary<string, string> FillComparerBag(string fileName)
        {
            var str = File.ReadLines(fileName);
            var cnv = str.Select(str =>
            {
                var spl = str.Split(";");
                return new KeyValuePair<string, string>(spl[0], spl[1]);
            });

            return new ConcurrentDictionary<string, string>(cnv);
        }


        private async Task<Option<PowerpointElasticDocument>> FilterExistingUnchanged(
            Option<PowerpointElasticDocument> document, ConcurrentDictionary<string, string> comparerBag)
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

                    if (currentHash == value) return Option.None<PowerpointElasticDocument>();
                    {
                        comparerBag.AddOrUpdate(doc.Id, currentHash, (key, innerValue) => innerValue);
                        return Option.Some(doc);
                    }
                });
                return opt;
            });
        }

        static readonly SHA256 sha256 = SHA256.Create();

        private async Task<Option<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
            ConfigurationObject configurationObject)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var wd = PresentationDocument.Open(currentFile, false);

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
                    var contentType = "pptx";
                    var lastPrinted = fInfo.LastPrinted ?? new DateTime(1970, 1, 1);
                    var lastModifiedBy = fInfo.LastModifiedBy.SomeNotNull().ValueOr("");
                    var uriPath = currentFile
                        .Replace(configurationObject.ScanPath, @"https://risprepository:8800/svns/PNR/extern")
                        .Replace(@"\", "/");

                    var idAsByte = sha256.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                    var id = Convert.ToBase64String(idAsByte);
                    var slideCount = wd.PresentationPart.SlideParts.Count();

                    var elements = wd
                        .PresentationPart
                        .SlideParts
                        .Select(p =>
                        {
                            var slide = p.Slide;

                            var commentArray = p.SlideCommentsPart.SomeNotNull().Match(
                                some: comments =>
                                {
                                    return comments.CommentList.Select(comment =>
                                    {
                                        var d = (Comment) comment;

                                        var retValue = new OfficeDocumentComment();
                                        var dat = d.DateTime != null
                                            ? d.DateTime.Value.SomeNotNull().ValueOr(new DateTime(1970, 1, 1))
                                            : new DateTime(1970, 1, 1);

                                        retValue.Comment = d.Text.Text;
                                        retValue.Date = dat;

                                        return retValue;
                                    }).ToArray();
                                },
                                none: Array.Empty<OfficeDocumentComment>
                            );


                            var innerString = GetChildElements(slide.ChildElements);
                            var toReplaced = new List<(string, string)>();

                            ReplaceSpecialStringsTailR(ref innerString, toReplaced);
                            return new Tuple<string, OfficeDocumentComment[]>(innerString, commentArray);
                        });

                    var contentString = string.Join(" ", elements.Select(d => d.Item1));
                    var commentsArray = elements.Select(d => d.Item2).SelectMany(l => l).Distinct();

                    var commentsOnlyList = commentsArray.Select(d => d.Comment.Split(" "));

                    var keywordsList = keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

                    var listElementsToHash = new List<string>()
                    {
                        category, created.ToString(CultureInfo.CurrentCulture), contentString, creator, description,
                        identifier,
                        string.Join("", keywords), language, modified.ToString(CultureInfo.CurrentCulture), revision,
                        subject, title, version,
                        contentStatus, contentType, lastPrinted.ToString(CultureInfo.CurrentCulture), lastModifiedBy
                    };

                    var res = listElementsToHash.Concat(commentsOnlyList.SelectMany(k => k).Distinct());

                    var contentHashString = CreateHashString(res);

                    var commString = string.Join(" ", commentsArray.Select(d => d.Comment));


                    var suggestedText = Regex.Replace(contentString + " " + commString, "[^a-zA-Zäöüß]", " ");
                    var searchAsYouTypeContent = suggestedText
                        .ToLower()
                        .Split(" ")
                        .Distinct()
                        .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                        .Where(d => d.Length > 2);

                    var completionField = new CompletionField {Input = searchAsYouTypeContent};

                    var returnValue = new PowerpointElasticDocument()
                    {
                        Category = category, CompletionContent = completionField, Content = contentString,
                        ContentHash = contentHashString, ContentStatus = contentStatus, ContentType = contentType,
                        Created = created, Creator = creator, Description = description, Id = id,
                        Identifier = identifier, Keywords = keywordsList, Language = language, Modified = modified,
                        Revision = revision, Subject = subject, Title = title, Version = version,
                        LastPrinted = lastPrinted, ProcessTime = DateTime.Now, LastModifiedBy = lastModifiedBy,
                        OriginalFilePath = currentFile, UriFilePath = uriPath, SlideCount = slideCount,
                        Comments = commentsArray
                    };

                    return Option.Some(returnValue);
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"An error while creating a indexing object at <{currentFile}>");
                return await Task.Run(Option.None<PowerpointElasticDocument>);
            }
        }

        private static string CreateHashString(IEnumerable<string> elements)
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