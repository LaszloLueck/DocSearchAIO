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
using Akka.Util.Internal;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
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
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly StatisticUtilities<WordElasticDocument> _statisticUtilities;
        private readonly ComparersBase<WordElasticDocument> _comparers;
        private readonly JobStateMemoryCache<WordElasticDocument> _jobStateMemoryCache;

        public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase,
            IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);

            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.WordStatisticUtility(loggerFactory, liteDatabase);
            _comparers = new ComparersBase<WordElasticDocument>(loggerFactory, _cfg);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetWordJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var schedulerEntry = _cfg.Processing[nameof(WordElasticDocument)];

                schedulerEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                schedulerEntry.TriggerName,
                                _cfg.GroupName);
                            _logger.LogWarning(
                                "skip processing of word documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            var materializer = _actorSystem.Materializer();
                            _logger.LogInformation("start job");
                            var indexName =
                                _schedulerUtilities.CreateIndexName(_cfg.IndexName, schedulerEntry.IndexSuffix);

                            await _schedulerUtilities.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

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
                                                    fileName => ProcessWordDocument(fileName, _cfg))
                                                .SelectAsync(parallelism: schedulerEntry.Parallelism,
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
                                            _logger.LogInformation("finished processing word-documents");
                                            sw.Stop();
                                            jobStatistic.EndJob = DateTime.Now;
                                            jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                                            jobStatistic.EntireDocCount = _statisticUtilities.GetEntireDocumentsCount();
                                            jobStatistic.ProcessingError =
                                                _statisticUtilities.GetFailedDocumentsCount();
                                            jobStatistic.IndexedDocCount =
                                                _statisticUtilities.GetChangedDocumentsCount();
                                            _statisticUtilities
                                                .AddJobStatisticToDatabase(jobStatistic);
                                            _logger.LogInformation("index documents in {ElapsedTimeMs} ms",
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
                        });
            });
        }

        private async Task<Maybe<WordElasticDocument>> ProcessWordDocument(string currentFile,
            ConfigurationObject configurationObject)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var md5 = MD5.Create();
                    var wdOpt = WordprocessingDocument.Open(currentFile, false).MaybeValue();
                    return await wdOpt.Match(
                        async wd =>
                        {
                            var mainDocumentPartOpt = wd.MainDocumentPart.MaybeValue();
                            return await mainDocumentPartOpt
                                .Match(
                                    async mainDocumentPart =>
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
                                        const string contentType = "docx";
                                        var lastPrinted = fInfo.LastPrinted ?? new DateTime(1970, 1, 1);
                                        var lastModifiedBy = fInfo.LastModifiedBy.MaybeValue().Unwrap("");
                                        var uriPath = currentFile
                                            .Replace(configurationObject.ScanPath,
                                                _cfg.UriReplacement)
                                            .Replace(@"\", "/");

                                        var idAsByte = md5.ComputeHash(Encoding.UTF8.GetBytes(currentFile));
                                        var id = BitConverter.ToString(idAsByte);

                                        var commentArray = mainDocumentPart
                                            .WordprocessingCommentsPart
                                            .MaybeValue()
                                            .Match(
                                                comments =>
                                                {
                                                    return comments.Comments.Select(comment =>
                                                    {
                                                        var d = (Comment) comment;
                                                        var retValue = new OfficeDocumentComment();
                                                        var dat = d.Date != null
                                                            ? d.Date.Value
                                                                .MaybeValue()
                                                                .Unwrap(new DateTime(1970, 1, 1))
                                                            : new DateTime(1970, 1, 1);

                                                        retValue.Author = d.Author?.Value;
                                                        retValue.Comment = d.InnerText;
                                                        retValue.Date = dat;
                                                        retValue.Id = d.Id?.Value;
                                                        retValue.Initials = d.Initials?.Value;
                                                        return retValue;
                                                    }).ToArray();
                                                },
                                                Array.Empty<OfficeDocumentComment>);

                                        var elements = mainDocumentPart
                                            .Document
                                            .Body?
                                            .ChildElements;

                                        var sw = new StringBuilder(4096);
                                        ExtractTextFromElements(elements, sw);
                                        var contentString = sw.ToString();
                                        sw.Clear();


                                        var toReplaced = new List<(string, string)>();

                                        ReplaceSpecialStringsTailR(ref contentString, toReplaced);

                                        var keywordsList = keywords.Length == 0
                                            ? Array.Empty<string>()
                                            : keywords.Split(",");

                                        var commentsString = commentArray.Select(l => l.Comment.Split(" ")).Distinct()
                                            .ToList();

                                        var listElementsToHash = new List<string>
                                        {
                                            category, created.ToString(CultureInfo.CurrentCulture), contentString,
                                            creator,
                                            description,
                                            identifier,
                                            string.Join("", keywords), language,
                                            modified.ToString(CultureInfo.CurrentCulture),
                                            revision,
                                            subject, title, version,
                                            contentStatus, contentType,
                                            lastPrinted.ToString(CultureInfo.CurrentCulture),
                                            lastModifiedBy
                                        };

                                        var res = listElementsToHash.Concat(
                                            commentsString.SelectMany(k => k).Distinct());

                                        var contentHashString = await _schedulerUtilities.CreateHashString(res);

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

                                        var returnValue = new WordElasticDocument
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
                                            OriginalFilePath = currentFile, UriFilePath = uriPath,
                                            Comments = commentArray
                                        };

                                        return Maybe<WordElasticDocument>.From(returnValue);
                                    },
                                    async () =>
                                    {
                                        _logger.LogWarning(
                                            "cannot process maindocumentpart of file {CurrentFile}, because it is null",
                                            currentFile);
                                        return await Task.Run(() => Maybe<WordElasticDocument>.None);
                                    });
                        },
                        async () =>
                        {
                            _logger.LogWarning(
                                "cannot process the basedocument of file {CurrentFile}, because it is null",
                                currentFile);
                            return await Task.Run(() => Maybe<WordElasticDocument>.None);
                        });
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "an error while creating a indexing object");
                _statisticUtilities.AddToFailedDocuments();
                return await Task.Run(() => Maybe<WordElasticDocument>.None);
            }
        }


        private static void ExtractTextFromElements(IEnumerable<OpenXmlElement> list, StringBuilder sw)
        {
            list
                .ForEach(element =>
                {
                    switch (element.LocalName)
                    {
                        case "t" when !element.ChildElements.Any():
                            sw.Append(element.InnerText);
                            break;
                        case "p":
                            ExtractTextFromElements(element.ChildElements, sw);
                            sw.Append(' ');
                            break;
                        case "br":
                            sw.Append(' ');
                            break;
                        default:
                            ExtractTextFromElements(element.ChildElements, sw);
                            break;
                    }
                });
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