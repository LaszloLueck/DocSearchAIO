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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Optional;
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
        private readonly SchedulerUtils _schedulerUtils;

        public OfficePowerpointProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<OfficePowerpointProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtils = new SchedulerUtils(loggerFactory, elasticSearchService, liteDatabase);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var schedulerEntry = _cfg.Processing["powerpoint"];
            await Task.Run(() =>
            {
                schedulerEntry
                    .Active
                    .Either(
                        new { }, 
                        new { },
                        async _ =>
                        {
                            await _schedulerUtils.SetTriggerStateByUserAction(context.Scheduler,
                                schedulerEntry.TriggerName,
                                _cfg.GroupName);
                            _logger.LogWarning(
                                "skip processing of powerpoint documents because the scheduler is inactive per config");
                        },
                        async _ =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName = _schedulerUtils.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtils.CheckAndCreateElasticIndex<ElasticDocument>(indexName);

                            _logger.LogInformation("start crunching and indexing some powerpoint documents");
                            Directory
                                .Exists(_cfg.ScanPath)
                                .Either(
                                    _cfg.ScanPath,
                                    _cfg.ScanPath,
                                    scanPath =>
                                    {
                                        _logger.LogWarning(
                                            "directory to scan <{ScanPath}> does not exists. skip working", scanPath);
                                    },
                                    async scanPath =>
                                    {
                                        var sw = Stopwatch.StartNew();
                                        var runnable = Source
                                            .From(Directory.GetFiles(_cfg.ScanPath, schedulerEntry.FileExtension,
                                                SearchOption.AllDirectories))
                                            .Where(file =>
                                                _schedulerUtils.UseExcludeFileFilter(schedulerEntry.ExcludeFilter, file))
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                fileName => ProcessPowerpointDocument(fileName, _cfg))
                                            .SelectAsync(parallelism: schedulerEntry.Parallelism,
                                                elementOpt => _schedulerUtils.FilterExistingUnchanged(elementOpt))
                                            .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                            .WithOptionFilter()
                                            .SelectAsync(schedulerEntry.Parallelism,
                                                async processingInfo =>
                                                    await _elasticSearchService.BulkWriteDocumentsAsync(@processingInfo,
                                                        indexName))
                                            .RunWith(Sink.Ignore<bool>(), materializer);

                                        await Task.WhenAll(runnable);

                                        _logger.LogInformation("finished processing powerpoint documents");

                                        sw.Stop();
                                        _logger.LogInformation("index documents in {ElapsedMilliseconds} ms",
                                            sw.ElapsedMilliseconds);
                                    });
                        });
            });
        }

        private async Task<Option<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
            ConfigurationObject configurationObject)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var md5 = MD5.Create();
                    var wdOpt = PresentationDocument.Open(currentFile, false).SomeNotNull();
                    return await wdOpt.Map(async wd =>
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
                        var contentType = "pptx";
                        var lastPrinted = fInfo.LastPrinted ?? new DateTime(1970, 1, 1);
                        var lastModifiedBy = fInfo.LastModifiedBy.SomeNotNull().ValueOr("");
                        var uriPath = currentFile
                            .Replace(configurationObject.ScanPath, @"https://risprepository:8800/svns/PNR/extern")
                            .Replace(@"\", "/");

                        var idAsByte = md5.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                        var id = Convert.ToBase64String(idAsByte);
                        var slideCount = wd.PresentationPart.SomeNotNull().Map(part => part.SlideParts.Count())
                            .ValueOr(0);

                        var elements = wd
                            .PresentationPart?
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

                                            retValue.Comment = d.Text?.Text;
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

                        var enumerable = elements as Tuple<string, OfficeDocumentComment[]>[] ?? elements.SomeNotNull()
                            .Map(element => element.ToArray())
                            .ValueOr(Array.Empty<Tuple<string, OfficeDocumentComment[]>>);
                        var contentString = string.Join(" ", enumerable.Select(d => d.Item1));
                        var commentsArray = enumerable.Select(d => d.Item2).SelectMany(l => l).Distinct();

                        var officeDocumentComments =
                            commentsArray as OfficeDocumentComment[] ?? commentsArray.ToArray();
                        var commentsOnlyList = officeDocumentComments.Select(d => d.Comment.Split(" "));

                        var keywordsList = keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

                        var listElementsToHash = new List<string>()
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

                        var contentHashString = await _schedulerUtils.CreateHashString(res);

                        var commString = string.Join(" ", officeDocumentComments.Select(d => d.Comment));


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

                        return Option.Some(returnValue);
                    }).ValueOr(async () =>
                    {
                        _logger.LogWarning("cannot process the basedocument of file {CurrentFile}, because it is null",
                            currentFile);
                        return await Task.Run(() => Option.None<PowerpointElasticDocument>());
                    });
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "an error while creating a indexing object at <{CurrentFile}>", currentFile);
                return await Task.Run(Option.None<PowerpointElasticDocument>);
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
            if (!replaceList.Any())
                return;

            input = Regex.Replace(input, replaceList[0].Item1, replaceList[0].Item2);
            replaceList.RemoveAt(0);
            ReplaceSpecialStringsTailR(ref input, replaceList);
        }
    }
}