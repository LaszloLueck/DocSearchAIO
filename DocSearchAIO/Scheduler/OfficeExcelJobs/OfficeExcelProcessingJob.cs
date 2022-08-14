using System.Diagnostics;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficeExcelJobs;

[DisallowConcurrentExecution]
public class OfficeExcelProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelExcel> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelExcel> _jobStateMemoryCache;
    private readonly IElasticUtilities _elasticUtilities;

    public OfficeExcelProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService,
        IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<OfficeExcelProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _statisticUtilities = StatisticUtilitiesProxy.ExcelStatisticUtility(loggerFactory,
            TypedDirectoryPathString.New(_cfg.StatisticsDirectory),
            new StatisticModelExcel().StatisticFileName);
        _comparerModel = new ComparerModelExcel(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache = JobStateMemoryCacheProxy.GetExcelJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }


    public async Task Execute(IJobExecutionContext context)
    {
        var configEntry = _cfg.Processing[nameof(ExcelElasticDocument)];
        await Task.Run(async () =>
        {
            var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelExcelCleanup());
            if (cacheEntryOpt.IsSome &&
                (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
            {
                _logger.LogInformation(
                    "cannot execute scanning and processing documents, opponent job cleanup running");
                return;
            }


            if (!configEntry.Active)
            {
                await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                    configEntry.TriggerName,
                    _cfg.SchedulerGroupName, TriggerState.Paused);
                _logger.LogWarning(
                    "skip processing of word documents because the scheduler is inactive per config");
            }
            else
            {
                _logger.LogInformation("start job");
                var indexName =
                    _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);
                await _elasticUtilities.CheckAndCreateElasticIndex<ExcelElasticDocument>(indexName);
                _logger.LogInformation("start crunching and indexing some excel-documents");
                if (!Directory.Exists(_cfg.ScanPath))
                {
                    _logger.LogWarning(
                        "directory to scan <{ScanPath}> does not exists. skip working",
                        _cfg.ScanPath);
                }
                else
                {
                    try
                    {
                        _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                        var jobStatistic = new ProcessingJobStatistic
                        {
                            Id = Guid.NewGuid().ToString(), StartJob = DateTime.Now
                        };
                        var sw = Stopwatch.StartNew();

                        await TypedFilePathString.New(_cfg.ScanPath)
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
                        jobStatistic.EntireDocCount = _statisticUtilities.EntireDocumentsCount();
                        jobStatistic.ProcessingError =
                            _statisticUtilities.FailedDocumentsCount();
                        jobStatistic.IndexedDocCount =
                            _statisticUtilities.ChangedDocumentsCount();
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
                }
            }
        });
    }
}

internal static class ExcelProcessingHelper
{
    public static Source<Option<ExcelElasticDocument>, NotUsed> ProcessExcelDocumentAsync(
        this Source<string, NotUsed> source, SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelExcel> statisticUtilities, ILogger logger)
    {
        return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
            f => ProcessingExcelDocument(f, configurationObject, statisticUtilities, logger));
    }

    private static async Task<Option<ExcelElasticDocument>> ProcessingExcelDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelExcel> statisticUtilities,
        ILogger logger)
    {
        try
        {
            var wdOpt = SpreadsheetDocument.Open(currentFile, false);
            Option<WorkbookPart> workBookPartOpt = wdOpt.WorkbookPart!;

            if (workBookPartOpt.IsNone)
            {
                return Option<ExcelElasticDocument>.None;
            }

            var mainWorkbookPart = workBookPartOpt.ValueUnsafe();
            var fInfo = wdOpt.PackageProperties;
            var category = fInfo.Category.IfNull(string.Empty);
            var created = fInfo.Created.IfNone(new DateTime(1970, 1, 1));
            var creator = fInfo.Creator.IfNull(string.Empty);
            var description = fInfo.Description.IfNull(string.Empty);
            var identifier = fInfo.Identifier.IfNull(string.Empty);
            var keywords = fInfo.Keywords.IfNull(string.Empty);
            var language = fInfo.Language.IfNull(string.Empty);
            var modified = fInfo.Modified.IfNone(new DateTime(1970, 1, 1));
            var revision = fInfo.Revision.IfNull(string.Empty);
            var subject = fInfo.Subject.IfNull(string.Empty);
            var title = fInfo.Title.IfNull(string.Empty);
            var version = fInfo.Version.IfNull(string.Empty);
            var contentStatus = fInfo.ContentStatus.IfNull(string.Empty);
            const string contentType = "xlsx";
            var lastPrinted = fInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = fInfo.LastModifiedBy.IfNull(string.Empty);
            var uriPath = currentFile
                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var id = await StaticHelpers.CreateHashString(
                TypedHashedInputString.New(currentFile));

            static IEnumerable<OfficeDocumentComment>
                CommentArray(WorkbookPart workbookPart) =>
                workbookPart
                    .WorksheetParts
                    .CommentsFromDocument();

            OfficeDocumentComment[] commentsArray = CommentArray(mainWorkbookPart).ToArray();

            var toReplaced = new List<(string, string)>()
            {
                (@"\r\n?|\n", ""),
                ("[ ]{2,}", " ")
            };

            var contentString = mainWorkbookPart
                .SharedStringTablePart
                .ResolveNullable(string.Empty,
                    (v, _) =>
                        Elements(v)
                            .ContentString()
                            .ReplaceSpecialStrings(toReplaced));

            var toHash = new ElementsToHash(category, created, contentString, creator,
                description, identifier, keywords, language, modified, revision,
                subject, title, version, contentStatus, contentType, lastPrinted,
                lastModifiedBy);

            var elementsHash = await (
                    StaticHelpers.ListElementsToHash(toHash), commentsArray)
                .ContentHashString();

            var completionField = commentsArray
                .StringFromCommentsArray()
                .GenerateTextToSuggest(TypedContentString.New(contentString))
                .GenerateSearchAsYouTypeArray()
                .WrapCompletionField();

            var returnValue = new ExcelElasticDocument
            {
                Category = category,
                CompletionContent = completionField,
                Content = contentString,
                ContentHash = elementsHash.Value,
                ContentStatus = contentStatus,
                ContentType = contentType,
                Created = created,
                Creator = creator,
                Description = description,
                Id = id.Value,
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
            return returnValue;
        }
        catch (Exception e)
        {
            logger.LogError(e, "an error while creating a indexing object");
            statisticUtilities.AddToFailedDocuments();
            return await Task.Run(() => Option<ExcelElasticDocument>.None);
        }
    }

    private static IEnumerable<OfficeDocumentComment>
        ConvertToOfficeDocumentComment(this CommentList comments)
    {
        return comments.ChildElements.Map(comment => OfficeDocumentComment((Comment) comment));
    }

    private static OfficeDocumentComment OfficeDocumentComment(Comment comment) =>
        new()
        {
            Comment = comment.CommentText.ResolveNullable(string.Empty, (v, _) => v.InnerText)
        };

    private static IEnumerable<OfficeDocumentComment>
        CommentsFromDocument(this IEnumerable<WorksheetPart> worksheets) =>
        worksheets
            .Map(part =>
            {
                var officeDocumentCommentsEmpty = System.Array.Empty<OfficeDocumentComment>();
                return part
                    .WorksheetCommentsPart
                    .ResolveNullable(officeDocumentCommentsEmpty, (commentsPart, _) =>
                    {
                        return commentsPart
                            .Comments
                            .CommentList
                            .ResolveNullable(officeDocumentCommentsEmpty,
                                (comment, _) => ConvertToOfficeDocumentComment(comment).ToArray());
                    });
            })
            .Flatten();

    private static IEnumerable<OpenXmlElement> Elements(this SharedStringTablePart sharedStringTablePart)
    {
        return sharedStringTablePart.SharedStringTable;
    }
}