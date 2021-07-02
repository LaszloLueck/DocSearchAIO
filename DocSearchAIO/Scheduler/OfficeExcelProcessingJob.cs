using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class OfficeExcelProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly StatisticUtilities<StatisticModelExcel> _statisticUtilities;
        private readonly ComparerModel _comparerModel;
        private readonly JobStateMemoryCache<MemoryCacheModelExcel> _jobStateMemoryCache;
        private readonly ElasticUtilities _elasticUtilities;
        
        public OfficeExcelProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService,
            IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficeExcelProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.ExcelStatisticUtility(loggerFactory, _cfg.StatisticsDirectory,
                new StatisticModelExcel().GetStatisticFileName);
            _comparerModel = new ComparerModelExcel(loggerFactory, _cfg.ComparerDirectory);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetExcelJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }


        public async Task Execute(IJobExecutionContext context)
        {
            var configEntry = _cfg.Processing[nameof(ExcelElasticDocument)];
            await Task.Run(() =>
            {
                var cacheEntryOpt = _jobStateMemoryCache.GetCacheEntry(new MemoryCacheModelExcelCleanup());
                if (!cacheEntryOpt.HasNoValue &&
                    (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                {
                    _logger.LogInformation("cannot execute scanning and processing documents, opponent job cleanup running");
                    return;
                }
                
                
                configEntry
                    .Active
                    .IfTrueFalse(async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.SchedulerGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip processing of word documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            _logger.LogInformation("start job");
                            var indexName =
                                _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);
                            await _elasticUtilities.CheckAndCreateElasticIndex<ExcelElasticDocument>(indexName);
                            _logger.LogInformation("start crunching and indexing some excel-documents");
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

                                            await new GenericSourceFilePath(scanPath)
                                                .CreateSource(configEntry.FileExtension)
                                                .UseExcludeFileFilter(configEntry.ExcludeFilter)
                                                .CountEntireDocs(_statisticUtilities)
                                                .ProcessExcelDocumentAsync(configEntry, _cfg, _statisticUtilities,
                                                    _logger)
                                                .FilterExistingUnchangedAsync(configEntry, _comparerModel)
                                                .GroupedWithin(50, TimeSpan.FromSeconds(10))
                                                .WithMaybeFilter()
                                                .CountFilteredDocs(_statisticUtilities)
                                                .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                                                    indexName)
                                                .RunIgnore(_actorSystem.Materializer());

                                            _logger.LogInformation("finished processing excel-documents");
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
                                            _logger.LogInformation("index documents in {ElapsedTimeMs} ms",
                                                sw.ElapsedMilliseconds);
                                            _comparerModel.RemoveComparerFile();
                                            await _comparerModel.WriteAllLinesAsync();
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
    }

    internal static class ExcelProcessingHelper
    {
        public static Source<Maybe<ExcelElasticDocument>, NotUsed> ProcessExcelDocumentAsync(
            this Source<string, NotUsed> source, SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
            StatisticUtilities<StatisticModelExcel> statisticUtilities, ILogger logger)
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                f => ProcessingExcelDocument(f, configurationObject, statisticUtilities, logger));
        }

        private static async Task<Maybe<ExcelElasticDocument>> ProcessingExcelDocument(string currentFile,
            ConfigurationObject configurationObject, StatisticUtilities<StatisticModelExcel> statisticUtilities,
            ILogger logger)
        {
            try
            {
                var wdOpt = SpreadsheetDocument.Open(currentFile, false).MaybeValue();
                return await wdOpt.Match(
                    async wd =>
                    {
                        var mainWorkbookPartOpt = wd.WorkbookPart.MaybeValue();
                        return await mainWorkbookPartOpt
                            .Match(
                                async mainWorkbookPart =>
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
                                    const string contentType = "xlsx";
                                    var lastPrinted =
                                        new GenericSourceNullable<DateTime>(fInfo.LastPrinted).ValueOrDefault(
                                            new DateTime(1970, 1, 1));
                                    var lastModifiedBy = fInfo.LastModifiedBy.ValueOr("");
                                    var uriPath = currentFile
                                        .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                                        .Replace(@"\", "/");

                                    var id = await StaticHelpers.CreateMd5HashString(currentFile);
                                    
                                    static IEnumerable<OfficeDocumentComment>
                                        CommentArray(WorkbookPart workbookPart) =>
                                        workbookPart?
                                            .WorksheetParts
                                            .CommentsFromDocument();

                                    var commentsArray = CommentArray(mainWorkbookPart).ToArray();

                                    var toReplaced = new List<(string, string)>();

                                    var contentString = mainWorkbookPart
                                        .SharedStringTablePart
                                        .Elements()
                                        .GetContentString()
                                        .ReplaceSpecialStrings(toReplaced);

                                    var elementsHash = await (
                                        StaticHelpers.GetListElementsToHash(category, created, contentString, creator,
                                            description, identifier, keywords, language, modified, revision,
                                            subject, title, version, contentStatus, contentType, lastPrinted,
                                            lastModifiedBy), commentsArray).GetContentHashString();

                                    var completionField = commentsArray
                                        .GetStringFromCommentsArray()
                                        .GenerateTextToSuggest(contentString)
                                        .GenerateSearchAsYouTypeArray()
                                        .WrapCompletionField();

                                    var returnValue = new ExcelElasticDocument
                                    {
                                        Category = category,
                                        CompletionContent = completionField,
                                        Content = contentString,
                                        ContentHash = elementsHash,
                                        ContentStatus = contentStatus,
                                        ContentType = contentType,
                                        Created = created,
                                        Creator = creator,
                                        Description = description,
                                        Id = id,
                                        Identifier = identifier,
                                        Keywords = StaticHelpers.KeywordsList(keywords),
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


                                    return Maybe<ExcelElasticDocument>.From(returnValue);
                                },
                                async () =>
                                {
                                    logger.LogWarning(
                                        "cannot process main document part of file {CurrentFile}, because it is null",
                                        currentFile);
                                    return await Task.Run(() => Maybe<ExcelElasticDocument>.None);
                                });
                    },
                    async () =>
                    {
                        logger.LogWarning(
                            "cannot process the base document of file {CurrentFile}, because it is null",
                            currentFile);
                        return await Task.Run(() => Maybe<ExcelElasticDocument>.None);
                    });
            }
            catch (Exception e)
            {
                logger.LogError(e, "an error while creating a indexing object");
                statisticUtilities.AddToFailedDocuments();
                return await Task.Run(() => Maybe<ExcelElasticDocument>.None);
            }
        }

        private static IEnumerable<OfficeDocumentComment>
            ConvertToOfficeDocumentComment(this CommentList comments)
        {
            return comments.ChildElements.Select(comment => OfficeDocumentComment((Comment)comment));
        }

        private static OfficeDocumentComment OfficeDocumentComment(Comment comment) =>
            new()
            {
                Comment = comment.CommentText?.InnerText
            };

        private static IEnumerable<OfficeDocumentComment>
            CommentsFromDocument(this IEnumerable<WorksheetPart> worksheets) =>
            worksheets
                .Select(part =>
                {
                    var officeDocumentCommentsEmpty = Array.Empty<OfficeDocumentComment>();
                    return part
                        .WorksheetCommentsPart
                        .MaybeValue()
                        .Match(
                            values =>
                            {
                                return values
                                    .Comments
                                    .CommentList
                                    .MaybeValue()
                                    .Match(
                                        ConvertToOfficeDocumentComment,
                                        () => officeDocumentCommentsEmpty
                                    );
                            },
                            () => officeDocumentCommentsEmpty);
                })
                .SelectMany(p => p);

        private static IEnumerable<OpenXmlElement> Elements(this SharedStringTablePart sharedStringTablePart)
        {
            if (sharedStringTablePart?.SharedStringTable is null)
                return ArraySegment<OpenXmlElement>.Empty;

            return sharedStringTablePart.SharedStringTable;
        }
    }
}