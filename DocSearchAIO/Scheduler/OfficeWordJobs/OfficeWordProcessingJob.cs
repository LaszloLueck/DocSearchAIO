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
using DocumentFormat.OpenXml.Wordprocessing;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;
using Quartz;
using Array = System.Array;

namespace DocSearchAIO.Scheduler.OfficeWordJobs;

[DisallowConcurrentExecution]
public class OfficeWordProcessingJob : IJob
{
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IConfigurationUpdater _configurationUpdater;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public OfficeWordProcessingJob(IConfigurationUpdater configurationUpdater,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService,
        IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _configurationUpdater = configurationUpdater;
        _memoryCache = memoryCache;
    }

    [Time]
    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<OfficeWordProcessingJob>();
        var configuration = await _configurationUpdater.ReadConfigurationAsync();

        var statisticUtilities = StatisticUtilitiesProxy.WordStatisticUtility(
            TypedDirectoryPathString.New(configuration.StatisticsDirectory),
            new StatisticModelWord().StatisticFileName);

        var comparerModel = new ComparerModelWord(configuration.ComparerDirectory);

        var jobStateMemoryCache = JobStateMemoryCacheProxy.GetWordJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();
        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelWordCleanup());
        if (cacheEntryOpt.IsSome &&
            (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }

        var configEntry = configuration.Processing[nameof(WordElasticDocument)];
        if (!configEntry.Active)
        {
            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                configEntry.TriggerName,
                configuration.SchedulerGroupName, TriggerState.Paused);
            logger.LogWarning(
                "skip processing of word documents because the scheduler is inactive per config");
        }
        else
        {
            logger.LogInformation("start job");
            var indexName =
                _elasticUtilities.CreateIndexName(configuration.IndexName, configEntry.IndexSuffix);

            await _elasticUtilities.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

            logger.LogInformation("start crunching and indexing some word-documents");

            if (!Directory.Exists(configuration.ScanPath))
            {
                logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists. skip working",
                    configuration.ScanPath);
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

                    await TypedFilePathString.New(configuration.ScanPath)
                        .CreateSource(configEntry.FileExtension)
                        .UseExcludeFileFilter(configEntry.ExcludeFilter)
                        .CountEntireDocs(statisticUtilities)
                        .ProcessWordDocumentAsync(configEntry, configuration, statisticUtilities, logger)
                        .FilterExistingUnchangedAsync(configEntry, comparerModel)
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .WithMaybeFilter()
                        .CountFilteredDocs(statisticUtilities)
                        .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                            indexName)
                        .RunIgnoreAsync(_actorSystem.Materializer());

                    logger.LogInformation("finished processing word-documents");
                    sw.Stop();
                    await _elasticSearchService.FlushIndexAsync(indexName);
                    await _elasticSearchService.RefreshIndexAsync(indexName);
                    jobStatistic.EndJob = DateTime.Now;
                    jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                    jobStatistic.EntireDocCount =
                        statisticUtilities.EntireDocumentsCount();
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

internal static class WordProcessingHelper
{
    public static Source<Option<WordElasticDocument>, NotUsed> ProcessWordDocumentAsync(
        this Source<string, NotUsed> source,
        SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelWord> statisticUtilities, ILogger logger)
    {
        return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
            f => ProcessWordDocument(f, configurationObject, statisticUtilities, logger));
    }

    private static readonly Lst<(string, string)> ToReplaced = List(
        (@"\r\n?|\n", ""),
        ("[ ]{2,}", " ")
    );

    private static async Task<Option<WordElasticDocument>> ProcessWordDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelWord> statisticUtilities,
        ILogger logger)
    {
        try
        {
            (Option<MainDocumentPart> MainDocumentPartOpt, PackageProperties FInfo, WordprocessingDocument Document)
                docTuple = await Task.Run(() =>
                {
                    var wdOpt = WordprocessingDocument.Open(currentFile, false);
                    Option<MainDocumentPart> mainDocumentPartOpt = wdOpt.MainDocumentPart!;
                    PackageProperties fInfo = wdOpt.PackageProperties;
                    return (mainDocumentPartOpt, fInfo, wdOpt);
                });


            if (docTuple.MainDocumentPartOpt.IsNone)
                return Option<WordElasticDocument>.None;
            var mainDocumentPart = docTuple.MainDocumentPartOpt.ValueUnsafe();


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
            const string contentType = "docx";
            var lastPrinted = docTuple.FInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = docTuple.FInfo.LastModifiedBy.IfNull(string.Empty);


            var uriPath = currentFile
                .Replace(configurationObject.ScanPath,
                    configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var id = await StaticHelpers.CreateHashString(
                TypedHashedInputString.New(currentFile));


            static IEnumerable<OfficeDocumentComment> CommentArray(
                MainDocumentPart mainDocumentPart)
            {
                var comments = mainDocumentPart
                    .WordprocessingCommentsPart
                    .ResolveNullable(new Comments(), (v, _) => v.Comments);

                return comments.Map(comment =>
                {
                    var d = (Comment) comment;

                    var retValue = new OfficeDocumentComment
                    {
                        Author = d.Author?.Value ?? string.Empty,
                        Comment = d.InnerText,
                        Date = d.Date?.Value ?? new DateTime(1970, 1, 1),
                        Id = d.Id?.Value ?? string.Empty,
                        Initials = d.Initials?.Value ?? string.Empty
                    };
                    return retValue;
                });
            }

            var contentTask = await mainDocumentPart.Elements().ContentStringAsync();

            var contentString = contentTask
                .ReplaceSpecialStrings(ToReplaced);

            OfficeDocumentComment[] commentsArray = CommentArray(mainDocumentPart).ToArray();

            docTuple.Document.Close();
            docTuple.Document.Dispose();

            var toHash = new ElementsToHash(category, created, contentString,
                creator,
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

            var returnValue = new WordElasticDocument
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
            return await Task.FromResult(Option<WordElasticDocument>.None);
        }
    }


    private static IEnumerable<OpenXmlElement> Elements(this
        MainDocumentPart mainDocumentPart)
    {
        return mainDocumentPart
            .Document
            .Body
            .ResolveNullable(Array.Empty<OpenXmlElement>(), (v, _) => v.ChildElements.ToArray());
    }
}