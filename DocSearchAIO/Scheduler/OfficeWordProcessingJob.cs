using System;
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
using DocSearchAIO.Statistics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class OfficeWordProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerUtils _schedulerUtils;
        private readonly StatisticUtilities<WordElasticDocument> _statisticUtilities;

        public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);

            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtils = new SchedulerUtils(loggerFactory, elasticSearchService, liteDatabase);
            _statisticUtilities = new StatisticUtilities<WordElasticDocument>(loggerFactory, liteDatabase);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing["word"];
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
                                "skip processing of word documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName = _schedulerUtils.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtils.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

                            _logger.LogInformation("start crunching and indexing some word-documents");

                            Directory
                                .Exists(_cfg.ScanPath)
                                .IfTrueFalse(
                                    (_cfg.ScanPath, _cfg.ScanPath),
                                    scanPath =>
                                    {
                                        _logger.LogWarning(
                                            "directory to scan <{ScanPath}> does not exists. skip working",
                                            scanPath);
                                    },
                                    async scanPath =>
                                    {
                                        var jobStatistic = new ProcessingJobStatistic
                                        {
                                            Id = Guid.NewGuid().ToString(), StartJob = DateTime.Now
                                        };
                                        var entireDocs = new InterlockedCounter();
                                        var missedDocs = new InterlockedCounter();
                                        var indexedDocs = new InterlockedCounter();
                                        
                                        var sw = Stopwatch.StartNew();
                                        var runnable = Source
                                            .From(Directory.GetFiles(scanPath, schedulerEntry.FileExtension,
                                                SearchOption.AllDirectories))
                                            .Where(file =>
                                                _schedulerUtils.UseExcludeFileFilter(schedulerEntry.ExcludeFilter,
                                                    file))
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                fileName => ProcessWordDocument(fileName, _cfg, entireDocs, missedDocs))
                                            .SelectAsync(parallelism: schedulerEntry.Parallelism,
                                                elementOpt => _schedulerUtils.FilterExistingUnchanged(elementOpt))
                                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                            .WithOptionFilter()
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                async processingInfo =>
                                                {
                                                    var values = processingInfo.ToList();
                                                    indexedDocs.Add(values.Count);
                                                    return await _elasticSearchService.BulkWriteDocumentsAsync(values,
                                                        indexName);
                                                })
                                            .RunWith(Sink.Ignore<bool>(), materializer);

                                        await Task.WhenAll(runnable);
                                        _logger.LogInformation("finished processing word-documents");

                                        sw.Stop();
                                        jobStatistic.EndJob = DateTime.Now;
                                        jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                                        jobStatistic.EntireDocCount = entireDocs.GetCurrent();
                                        jobStatistic.ProcessingError = missedDocs.GetCurrent();
                                        jobStatistic.IndexedDocCount = indexedDocs.GetCurrent();
                                        _statisticUtilities.AddJobStatisticToDatabase(jobStatistic);
                                        _logger.LogInformation("index documents in {ElapsedTimeMs} ms",
                                            sw.ElapsedMilliseconds);
                                    });
                        });
            });
        }

        private async Task<Option<WordElasticDocument>> ProcessWordDocument(string currentFile,
            ConfigurationObject configurationObject, InterlockedCounter entireDocs, InterlockedCounter missedDocs)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var md5 = MD5.Create();
                    entireDocs.Increment();
                    var wdOpt = WordprocessingDocument.Open(currentFile, false).SomeNotNull();
                    return await wdOpt.Map(async wd =>
                        {
                            var mainDocumentPartOpt = wd.MainDocumentPart.SomeNotNull();
                            return await mainDocumentPartOpt.Map(async mainDocumentPart =>
                                {
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
                                        .Replace(configurationObject.ScanPath,
                                            _cfg.UriReplacement)
                                        .Replace(@"\", "/");

                                    var idAsByte = md5.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                                    var id = Convert.ToBase64String(idAsByte);

                                    var commentArray = mainDocumentPart
                                        .WordprocessingCommentsPart
                                        .SomeNotNull()
                                        .Match(
                                            some: comments =>
                                            {
                                                return comments.Comments.Select(comment =>
                                                {
                                                    var d = (Comment) comment;
                                                    var retValue = new OfficeDocumentComment();
                                                    var dat = d.Date != null
                                                        ? d.Date.Value.SomeNotNull().ValueOr(new DateTime(1970, 1, 1))
                                                        : new DateTime(1970, 1, 1);

                                                    retValue.Author = d.Author?.Value;
                                                    retValue.Comment = d.InnerText;
                                                    retValue.Date = dat;
                                                    retValue.Id = d.Id?.Value;
                                                    retValue.Initials = d.Initials?.Value;
                                                    return retValue;
                                                }).ToArray();
                                            },
                                            none: Array.Empty<OfficeDocumentComment>
                                        );

                                    var elements = mainDocumentPart
                                        .Document
                                        .Body?
                                        .ChildElements;

                                    var contentString = GetChildElements(elements);
                                    var toReplaced = new List<(string, string)>();

                                    ReplaceSpecialStringsTailR(ref contentString, toReplaced);

                                    var keywordsList = keywords.Length == 0
                                        ? Array.Empty<string>()
                                        : keywords.Split(",");

                                    var commentsString = commentArray.Select(l => l.Comment.Split(" ")).Distinct()
                                        .ToList();

                                    var listElementsToHash = new List<string>
                                    {
                                        category, created.ToString(CultureInfo.CurrentCulture), contentString, creator,
                                        description,
                                        identifier,
                                        string.Join("", keywords), language,
                                        modified.ToString(CultureInfo.CurrentCulture),
                                        revision,
                                        subject, title, version,
                                        contentStatus, contentType, lastPrinted.ToString(CultureInfo.CurrentCulture),
                                        lastModifiedBy
                                    };

                                    var res = listElementsToHash.Concat(commentsString.SelectMany(k => k).Distinct());

                                    var contentHashString = await _schedulerUtils.CreateHashString(res);

                                    var commString = string.Join(" ", commentArray.Select(d => d.Comment));
                                    var suggestedText =
                                        Regex.Replace(contentString + " " + commString, "[^a-zA-ZäöüßÄÖÜ]", " ");


                                    var searchAsYouTypeContent = suggestedText
                                        .ToLower()
                                        .Split(" ")
                                        .Distinct()
                                        .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                                        .Where(d => d.Length > 2);

                                    var completionField = new CompletionField {Input = searchAsYouTypeContent};

                                    var returnValue = new WordElasticDocument()
                                    {
                                        Category = category, CompletionContent = completionField,
                                        Content = contentString,
                                        ContentHash = contentHashString, ContentStatus = contentStatus,
                                        ContentType = contentType,
                                        Created = created, Creator = creator, Description = description, Id = id,
                                        Identifier = identifier, Keywords = keywordsList, Language = language,
                                        Modified = modified,
                                        Revision = revision, Subject = subject, Title = title, Version = version,
                                        LastPrinted = lastPrinted, ProcessTime = DateTime.Now,
                                        LastModifiedBy = lastModifiedBy,
                                        OriginalFilePath = currentFile, UriFilePath = uriPath, Comments = commentArray
                                    };

                                    return Option.Some(returnValue);
                                })
                                .ValueOr(async () =>
                                {
                                    _logger.LogWarning(
                                        "cannot process maindocumentpart of file {CurrentFile}, because it is null",
                                        currentFile);
                                    return await Task.Run(Option.None<WordElasticDocument>);
                                });
                        })
                        .ValueOr(async () =>
                        {
                            _logger.LogWarning(
                                "cannot process the basedocument of file {CurrentFile}, because it is null",
                                currentFile);
                            return await Task.Run(Option.None<WordElasticDocument>);
                        });
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "an error while creating a indexing object");
                missedDocs.Increment();
                return await Task.Run(Option.None<WordElasticDocument>);
            }
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

        private static void ReplaceSpecialStringsTailR(ref string input, IList<(string, string)> replaceList)
        {
            while (true)
            {
                if (!replaceList.Any()) return;

                input = Regex.Replace(input, replaceList[0].Item1, replaceList[0].Item2);
                replaceList.RemoveAt(0);
            }
        }
    }
}