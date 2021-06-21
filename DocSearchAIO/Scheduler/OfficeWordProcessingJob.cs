using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using DocumentFormat.OpenXml.Wordprocessing;
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
        private readonly StatisticUtilities<StatisticModelWord> _statisticUtilities;
        private readonly ComparersBase<WordElasticDocument> _comparers;
        private readonly JobStateMemoryCache<MemoryCacheModelWord> _jobStateMemoryCache;

        public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService,
            IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);

            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.WordStatisticUtility(loggerFactory, _cfg.StatisticsDirectory,
                new StatisticModelWord().GetStatisticFileName);
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
                                                .RunWith(Sink.Ignore<bool>(), _actorSystem.Materializer());

                                            await Task.WhenAll(runnable);
                                            _logger.LogInformation("finished processing word-documents");
                                            sw.Stop();
                                            await _elasticSearchService.FlushIndexAsync(indexName);
                                            await _elasticSearchService.RefreshIndexAsync(indexName);
                                            jobStatistic.EndJob = DateTime.Now;
                                            jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                                            jobStatistic.EntireDocCount = _statisticUtilities.GetEntireDocumentsCount();
                                            jobStatistic.ProcessingError =
                                                _statisticUtilities.GetFailedDocumentsCount();
                                            jobStatistic.IndexedDocCount =
                                                _statisticUtilities.GetChangedDocumentsCount();
                                            _statisticUtilities
                                                .AddJobStatisticToDatabase(jobStatistic);
                                            _logger.LogInformation($"index documents in {sw.ElapsedMilliseconds} ms");
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
                                        var category = fInfo.Category.ValueOr("");
                                        var created =
                                            new GenericSourceNullable<DateTime>(fInfo.Created).ValueOrDefault(
                                                new DateTime(1970, 1, 1));
                                        var creator = fInfo.Creator.ValueOr("");
                                        var description = fInfo.Description.ValueOr("");
                                        var identifier = fInfo.Identifier.ValueOr("");
                                        var keywords = fInfo.Keywords.ValueOr("");
                                        var language = fInfo.Language.ValueOr("");
                                        var modified =
                                            new GenericSourceNullable<DateTime>(fInfo.Modified).ValueOrDefault(
                                                new DateTime(1970, 1, 1));
                                        var revision = fInfo.Revision.ValueOr("");
                                        var subject = fInfo.Subject.ValueOr("");
                                        var title = fInfo.Title.ValueOr("");
                                        var version = fInfo.Version.ValueOr("");
                                        var contentStatus = fInfo.ContentStatus.ValueOr("");
                                        const string contentType = "docx";
                                        var lastPrinted =
                                            new GenericSourceNullable<DateTime>(fInfo.LastPrinted).ValueOrDefault(
                                                new DateTime(1970, 1, 1));
                                        var lastModifiedBy = fInfo.LastModifiedBy.ValueOr("");
                                        var uriPath = currentFile
                                            .Replace(configurationObject.ScanPath,
                                                _cfg.UriReplacement)
                                            .Replace(@"\", "/");

                                        var id = await StaticHelpers.CreateMd5HashString(currentFile);

                                        static OfficeDocumentComment[] GetCommentArray(
                                            MainDocumentPart mainDocumentPart) =>
                                            mainDocumentPart
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

                                        static IEnumerable<OpenXmlElement> GetElements(
                                            MainDocumentPart mainDocumentPart)
                                        {
                                            if (mainDocumentPart.Document.Body == null)
                                                return Array.Empty<OpenXmlElement>();

                                            return mainDocumentPart
                                                .Document
                                                .Body?
                                                .ChildElements
                                                .OfType<OpenXmlElement>();
                                        }

                                        static string GetContentString(IEnumerable<OpenXmlElement> openXmlElementList)
                                        {
                                            var sw = new StringBuilder(4096);
                                            StaticHelpers.ExtractTextFromElement(openXmlElementList, sw);
                                            var s = sw.ToString();
                                            return s;
                                        }

                                        var contentString = GetContentString(GetElements(mainDocumentPart));
                                        var toReplaced = new List<(string, string)>();

                                        contentString = StaticHelpers.ReplaceSpecialStrings(contentString, toReplaced);

                                        static IEnumerable<string> GetKeywordsList(string keywords) =>
                                            keywords.Length == 0
                                                ? Array.Empty<string>()
                                                : keywords.Split(",");

                                        static List<string> GetListElementsToHash(string category, DateTime created,
                                            string contentString, string creator, string description, string identifier,
                                            string keywords, string language, DateTime modified, string revision,
                                            string subject, string title, string version, string contentStatus,
                                            string contentType, DateTime lastPrinted, string lastModifiedBy) =>
                                            new()
                                            {
                                                category,
                                                created.ToString(CultureInfo.CurrentCulture),
                                                contentString,
                                                creator,
                                                description,
                                                identifier,
                                                keywords,
                                                language,
                                                modified.ToString(CultureInfo.CurrentCulture),
                                                revision,
                                                subject, title, version,
                                                contentStatus, contentType,
                                                lastPrinted.ToString(CultureInfo.CurrentCulture),
                                                lastModifiedBy
                                            };

                                        var commentsArray = GetCommentArray(mainDocumentPart);

                                        static IEnumerable<string[]> GetCommentsString(
                                            IEnumerable<OfficeDocumentComment> commentsArray) =>
                                            commentsArray
                                                .Select(l => l.Comment.Split(" "))
                                                .Distinct()
                                                .ToList();

                                        static IEnumerable<string> BuildHashList(List<string> listElementsToHash,
                                            OfficeDocumentComment[] commentsArray) =>
                                            listElementsToHash
                                                .Concat(
                                                    GetCommentsString(commentsArray)
                                                        .SelectMany(k => k).Distinct());

                                        static async Task<string> GetContentHashString(List<string> listElementsToHash,
                                            OfficeDocumentComment[] commentsArray) =>
                                            await StaticHelpers.CreateMd5HashString(
                                                BuildHashList(listElementsToHash, commentsArray).JoinString(""));

                                        static string GetCommString(OfficeDocumentComment[] commentsArray) =>
                                            string.Join(" ", commentsArray.Select(d => d.Comment));

                                        static string GetSuggestedText(string contentString, string commString) =>
                                            Regex.Replace(contentString + " " + commString, "[^a-zA-ZäöüßÄÖÜ]", " ");


                                        static IEnumerable<string> GetSearchAsYouTypeContent(string suggestedText) =>
                                            suggestedText
                                                .ToLower()
                                                .Split(" ")
                                                .Distinct()
                                                .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                                                .Where(d => d.Length > 2);

                                        static CompletionField GetCompletionField(
                                            IEnumerable<string> searchAsYouTypeContent) =>
                                            new CompletionField {Input = searchAsYouTypeContent};

                                        var returnValue = new WordElasticDocument
                                        {
                                            Category = category,
                                            CompletionContent = GetCompletionField(
                                                GetSearchAsYouTypeContent(GetSuggestedText(contentString,
                                                    GetCommString(commentsArray)))),
                                            Content = contentString,
                                            ContentHash = await GetContentHashString(
                                                GetListElementsToHash(category, created, contentString, creator,
                                                    description, identifier, keywords, language, modified, revision,
                                                    subject, title, version, contentStatus, contentType, lastPrinted,
                                                    lastModifiedBy), commentsArray),
                                            ContentStatus = contentStatus,
                                            ContentType = contentType,
                                            Created = created,
                                            Creator = creator,
                                            Description = description,
                                            Id = id,
                                            Identifier = identifier,
                                            Keywords = GetKeywordsList(keywords),
                                            Language = language,
                                            Modified = modified,
                                            Revision = revision,
                                            Subject = subject,
                                            Title = title,
                                            Version = version,
                                            LastPrinted = lastPrinted,
                                            ProcessTime = DateTime.Now,
                                            LastModifiedBy = lastModifiedBy,
                                            OriginalFilePath = currentFile,
                                            UriFilePath = uriPath,
                                            Comments = commentsArray
                                        };

                                        return Maybe<WordElasticDocument>.From(returnValue);
                                    },
                                    async () =>
                                    {
                                        _logger.LogWarning(
                                            "cannot process main document part of file {CurrentFile}, because it is null",
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
    }
}