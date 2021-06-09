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
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class OfficePowerpointProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly StatisticUtilities _statisticUtilities;
        private readonly Comparers<PowerpointElasticDocument> _comparers;

        public OfficePowerpointProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<OfficePowerpointProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = new StatisticUtilities(loggerFactory, liteDatabase);
            _comparers = new Comparers<PowerpointElasticDocument>(liteDatabase);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing[nameof(PowerpointElasticDocument)];
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
                                "skip processing of powerpoint documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName =
                                _schedulerUtilities.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtilities.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

                            _logger.LogInformation("start crunching and indexing some powerpoint documents");
                            Directory
                                .Exists(_cfg.ScanPath)
                                .IfTrueFalse((_cfg.ScanPath, _cfg.ScanPath),
                                    scanPath =>
                                    {
                                        _logger.LogWarning(
                                            "directory to scan <{ScanPath}> does not exists. skip working", scanPath);
                                    },
                                    async scanPath =>
                                    {
                                        var jobStatistic = new ProcessingJobStatistic
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            StartJob = DateTime.Now
                                        };
                                        
                                        var entireDocs = new InterlockedCounter();
                                        var missedDocs = new InterlockedCounter();
                                        var indexedDocs = new InterlockedCounter();
                                        var sw = Stopwatch.StartNew();
                                        var runnable = Source
                                            .From(Directory.GetFiles(scanPath, schedulerEntry.FileExtension,
                                                SearchOption.AllDirectories))
                                            .Where(file =>
                                                _schedulerUtilities.UseExcludeFileFilter(schedulerEntry.ExcludeFilter,
                                                    file))
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                fileName => ProcessPowerpointDocument(fileName, _cfg, entireDocs,
                                                    missedDocs))
                                            .SelectAsync(parallelism: schedulerEntry.Parallelism,
                                                elementOpt => _comparers.FilterExistingUnchanged(elementOpt))
                                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                            .WithMaybeFilter()
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

                                        _logger.LogInformation("finished processing powerpoint documents");

                                        sw.Stop();
                                        jobStatistic.EndJob = DateTime.Now;
                                        jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                                        jobStatistic.EntireDocCount = entireDocs.GetCurrent();
                                        jobStatistic.ProcessingError = missedDocs.GetCurrent();
                                        jobStatistic.IndexedDocCount = indexedDocs.GetCurrent();
                                        _statisticUtilities.AddJobStatisticToDatabase<PowerpointElasticDocument>(
                                            jobStatistic);
                                        _logger.LogInformation("index documents in {ElapsedMilliseconds} ms",
                                            sw.ElapsedMilliseconds);
                                    });
                        });
            });
        }

        private async Task<Maybe<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
            ConfigurationObject configurationObject, InterlockedCounter entireDocs, InterlockedCounter missedDocs)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var md5 = MD5.Create();
                    entireDocs.Increment();
                    var wdOpt = PresentationDocument
                        .Open(currentFile, false)
                        .MaybeValue();

                    return await wdOpt.Match(
                        async wd =>
                        {
                            var fInfo = wd.PackageProperties;
                            var category = fInfo.Category.MaybeValue().Unwrap("");
                            var created = fInfo.Created ?? new DateTime(1970, 1, 1);
                            var creator = fInfo.Creator.MaybeValue().Unwrap("");
                            var description = fInfo.Description.MaybeValue().Unwrap("");
                            var identifier = fInfo.Identifier.MaybeValue().Unwrap("");
                            var keywords = fInfo.Keywords.MaybeValue().Unwrap("");
                            var language = fInfo.Language.MaybeValue().Unwrap("");
                            var modified = fInfo.Modified ?? new DateTime(1970, 1, 1);
                            var revision = fInfo.Revision.MaybeValue().Unwrap("");
                            var subject = fInfo.Subject.MaybeValue().Unwrap("");
                            var title = fInfo.Title.MaybeValue().Unwrap("");
                            var version = fInfo.Version.MaybeValue().Unwrap("");
                            var contentStatus = fInfo.ContentStatus.MaybeValue().Unwrap("");
                            var contentType = "pptx";
                            var lastPrinted = fInfo.LastPrinted ?? new DateTime(1970, 1, 1);
                            var lastModifiedBy = fInfo.LastModifiedBy.MaybeValue().Unwrap("");
                            var uriPath = currentFile
                                .Replace(configurationObject.ScanPath, _cfg.UriReplacement)
                                .Replace(@"\", "/");

                            var idAsByte = md5.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                            var id = Convert.ToBase64String(idAsByte);
                            var slideCount = wd
                                .PresentationPart
                                .MaybeValue()
                                .Match(
                                    part => part.SlideParts.Count(),
                                    () => 0);

                            var elements = wd
                                .PresentationPart?
                                .SlideParts
                                .Select(p =>
                                {
                                    var slide = p.Slide;

                                    var commentArray = p
                                        .SlideCommentsPart
                                        .MaybeValue()
                                        .Match(
                                            comments =>
                                            {
                                                return comments.CommentList.Select(comment =>
                                                {
                                                    var d = (Comment)comment;

                                                    var retValue = new OfficeDocumentComment();
                                                    var dat = d.DateTime != null
                                                        ? d.DateTime
                                                            .Value
                                                            .MaybeValue()
                                                            .Unwrap(new DateTime(1970, 1, 1))
                                                        : new DateTime(1970, 1, 1);

                                                    retValue.Comment = d.Text?.Text;
                                                    retValue.Date = dat;

                                                    return retValue;
                                                }).ToArray();
                                            },
                                            Array.Empty<OfficeDocumentComment>
                                        );


                                    var innerString = GetChildElements(slide.ChildElements);
                                    var toReplaced = new List<(string, string)>();

                                    ReplaceSpecialStringsTailR(ref innerString, toReplaced);
                                    return new Tuple<string, OfficeDocumentComment[]>(innerString, commentArray);
                                });

                            var enumerable = elements as Tuple<string, OfficeDocumentComment[]>[] ?? elements
                                .MaybeValue()
                                .Match(
                                    element => element.ToArray(),
                                    Array.Empty<Tuple<string, OfficeDocumentComment[]>>
                                );
                            var contentString = string.Join(" ", enumerable.Select(d => d.Item1));
                            var commentsArray = enumerable.Select(d => d.Item2).SelectMany(l => l).Distinct();

                            var officeDocumentComments =
                                commentsArray as OfficeDocumentComment[] ?? commentsArray.ToArray();
                            var commentsOnlyList = officeDocumentComments.Select(d => d.Comment.Split(" "));

                            var keywordsList = keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

                            var listElementsToHash = new List<string>
                            {
                                category, created.ToString(CultureInfo.CurrentCulture), contentString, creator,
                                description,
                                identifier,
                                string.Join("", keywords), language, modified.ToString(CultureInfo.CurrentCulture),
                                revision,
                                subject, title, version,
                                contentStatus, contentType, lastPrinted.ToString(CultureInfo.CurrentCulture),
                                lastModifiedBy
                            };

                            var res = listElementsToHash.Concat(commentsOnlyList.SelectMany(k => k).Distinct());

                            var contentHashString = await _schedulerUtilities.CreateHashString(res);

                            var commString = string.Join(" ", officeDocumentComments.Select(d => d.Comment));


                            var suggestedText = Regex.Replace(contentString + " " + commString, "[^a-zA-Zäöüß]", " ");
                            var searchAsYouTypeContent = suggestedText
                                .ToLower()
                                .Split(" ")
                                .Distinct()
                                .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                                .Where(d => d.Length > 2);

                            var completionField = new CompletionField { Input = searchAsYouTypeContent };

                            var returnValue = new PowerpointElasticDocument
                            {
                                Category = category, CompletionContent = completionField, Content = contentString,
                                ContentHash = contentHashString, ContentStatus = contentStatus,
                                ContentType = contentType,
                                Created = created, Creator = creator, Description = description, Id = id,
                                Identifier = identifier, Keywords = keywordsList, Language = language,
                                Modified = modified,
                                Revision = revision, Subject = subject, Title = title, Version = version,
                                LastPrinted = lastPrinted, ProcessTime = DateTime.Now, LastModifiedBy = lastModifiedBy,
                                OriginalFilePath = currentFile, UriFilePath = uriPath, SlideCount = slideCount,
                                Comments = officeDocumentComments
                            };

                            return Maybe<PowerpointElasticDocument>.From(returnValue);
                        },
                        async () =>
                        {
                            _logger.LogWarning(
                                "cannot process the basedocument of file {CurrentFile}, because it is null",
                                currentFile);
                            return await Task.Run(() => Maybe<PowerpointElasticDocument>.None);
                        });
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "an error while creating a indexing object at <{CurrentFile}>", currentFile);
                missedDocs.Increment();
                return await Task.Run(() => Maybe<PowerpointElasticDocument>.None);
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

        private void ReplaceSpecialStringsTailR(ref string input, IList<(string, string)> replaceList)
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