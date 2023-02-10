using System.Diagnostics;
using System.IO.Packaging;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;
using Quartz;
using Array = System.Array;

namespace DocSearchAIO.Scheduler.OfficeExcelJobs;

[DisallowConcurrentExecution]
public class OfficeExcelProcessingJob : IJob
{
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public OfficeExcelProcessingJob(IConfigurationUpdater configurationUpdater,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService,
        IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _configurationUpdater = configurationUpdater;
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _memoryCache = memoryCache;
    }


    [Time]
    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<OfficeExcelProcessingJob>();
        
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        
        var statisticUtilities = StatisticUtilitiesProxy.ExcelStatisticUtility(
            TypedDirectoryPathString.New(cfg.StatisticsDirectory),
            new StatisticModelExcel().StatisticFileName);
        var comparerModel = new ComparerModelExcel(cfg.ComparerDirectory);
        
        var configEntry = cfg.Processing[nameof(ExcelElasticDocument)];

        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetExcelJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();
        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelExcelCleanup());
        if (cacheEntryOpt.IsSome &&
            (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }


        if (!configEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                configEntry.TriggerName,
                cfg.SchedulerGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip processing of word documents because the scheduler is inactive per config");
        }
        else
        {
            logger.LogInformation("start job");
            var indexName =
                _elasticUtilities.CreateIndexName(cfg.IndexName, configEntry.IndexSuffix);
            await _elasticUtilities.CheckAndCreateElasticIndex<ExcelElasticDocument>(indexName);
            logger.LogInformation("start crunching and indexing some excel-documents");
            if (!Directory.Exists(cfg.ScanPath))
            {
                logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists. skip working",
                    cfg.ScanPath);
            }
            else
            {
                try
                {
                    jobStateMemoryCache.SetCacheEntry(JobState.Running);
                    var jobStatistic = new ProcessingJobStatistic
                    {
                        Id = Guid.NewGuid().ToString(),
                        StartJob = DateTime.Now
                    };
                    var sw = Stopwatch.StartNew();

                    await TypedFilePathString.New(cfg.ScanPath)
                        .CreateSource(configEntry.FileExtension)
                        .UseExcludeFileFilter(configEntry.ExcludeFilter)
                        .CountEntireDocs(statisticUtilities)
                        .ProcessExcelDocumentAsync(configEntry, cfg, statisticUtilities,
                            logger)
                        .FilterExistingUnchangedAsync(configEntry, comparerModel)
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .WithMaybeFilter()
                        .CountFilteredDocs(statisticUtilities)
                        .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                            indexName)
                        .RunIgnoreAsync(_actorSystem.Materializer());

                    logger.LogInformation("finished processing excel-documents");
                    sw.Stop();
                    await _elasticSearchService.FlushIndexAsync(indexName);
                    await _elasticSearchService.RefreshIndexAsync(indexName);
                    jobStatistic.EndJob = DateTime.Now;
                    jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                    jobStatistic.EntireDocCount = statisticUtilities.EntireDocumentsCount();
                    jobStatistic.ProcessingError =
                        statisticUtilities.FailedDocumentsCount();
                    jobStatistic.IndexedDocCount =
                        statisticUtilities.ChangedDocumentsCount();
                    statisticUtilities
                        .AddJobStatisticToDatabase(jobStatistic);
                    comparerModel.RemoveComparerFile();
                    await comparerModel.WriteAllLinesAsync();
                    jobStateMemoryCache.SetCacheEntry(JobState.Stopped);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error in processing pipeline occured");
                }
            }
        }
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

    private static readonly Lst<(string, string)> ToReplaced = List(
        (@"\r\n?|\n", ""),
        ("[ ]{2,}", " ")
    );

    private static async Task<Option<ExcelElasticDocument>> ProcessingExcelDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelExcel> statisticUtilities,
        ILogger logger)
    {
        try
        {

            (Option<WorkbookPart> WorkbookOption, PackageProperties FInfo, SpreadsheetDocument Document) docTuple =
                await Task.Run(() =>
                {
                    var wdOpt = SpreadsheetDocument.Open(currentFile, false);
                    Option<WorkbookPart> workBookPartOpt = wdOpt.WorkbookPart!;
                    var fInfo = wdOpt.PackageProperties;

                    return (workBookPartOpt, fInfo, wdOpt);
                });



            if (docTuple.WorkbookOption.IsNone)
                return Option<ExcelElasticDocument>.None;
            var mainWorkbookPart = docTuple.WorkbookOption.ValueUnsafe();


            var category = docTuple.FInfo.Category.IfNull(string.Empty);
            var created = docTuple.FInfo.Created.IfNone(new DateTime(1970, 1, 1));
            var creator = docTuple.FInfo.Creator.IfNull(string.Empty);
            var description = docTuple.FInfo.Description.IfNull(string.Empty);
            var identifier = docTuple.FInfo.Identifier.IfNull(string.Empty);
            var keywords = docTuple.FInfo.Keywords.IfNull(string.Empty);
            var language = docTuple.FInfo.Language.IfNull(string.Empty);
            var modified = docTuple.FInfo.Modified.IfNone(new DateTime(1970, 1, 1));
            var revision = docTuple.FInfo.Revision.IfNull(string.Empty);
            var subject = docTuple.FInfo.Subject.IfNull(string.Empty);
            var title = docTuple.FInfo.Title.IfNull(string.Empty);
            var version = docTuple.FInfo.Version.IfNull(string.Empty);
            var contentStatus = docTuple.FInfo.ContentStatus.IfNull(string.Empty);
            const string contentType = "xlsx";
            var lastPrinted = docTuple.FInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = docTuple.FInfo.LastModifiedBy.IfNull(string.Empty);
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


            var contentString = mainWorkbookPart.SharedStringTablePart != null
                ? (await Elements(mainWorkbookPart.SharedStringTablePart).ContentStringAsync()).ReplaceSpecialStrings(
                    ToReplaced)
                : string.Empty;

            docTuple.Document.Close();
            docTuple.Document.Dispose();

            var toHash = new ElementsToHash(category, created, contentString, creator,
                description, identifier, keywords, language, modified, revision,
                subject, title, version, contentStatus, contentType, lastPrinted,
                lastModifiedBy);

            var elementsHash = await (
                    StaticHelpers.ListElementsToHash(toHash), commentsArray)
                .ContentHashStringAsync();

            var tempVal = await commentsArray.StringFromCommentsArray()
                .GenerateTextToSuggestAsync(TypedContentString.New(contentString));

            var completionField = tempVal
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
            return await Task.FromResult(Option<ExcelElasticDocument>.None);
        }
    }

    private static IEnumerable<OfficeDocumentComment>
        ConvertToOfficeDocumentComment(this CommentList comments)
    {
        return comments.ChildElements.Map(comment => OfficeDocumentComment((Comment)comment));
    }

    private static OfficeDocumentComment OfficeDocumentComment(Comment comment) =>
        new()
        {
            Comment = comment.CommentText?.InnerText ?? string.Empty
        };

    private static IEnumerable<OfficeDocumentComment>
        CommentsFromDocument(this IEnumerable<WorksheetPart> worksheets) =>
        worksheets
            .Map(part =>
            {
                var officeDocumentCommentsEmpty = Array.Empty<OfficeDocumentComment>();
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