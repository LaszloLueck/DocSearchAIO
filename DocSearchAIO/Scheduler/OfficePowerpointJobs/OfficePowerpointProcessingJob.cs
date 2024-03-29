﻿using System.Diagnostics;
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
using DocumentFormat.OpenXml.Presentation;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;
using Nest;
using Quartz;
using Array = System.Array;

namespace DocSearchAIO.Scheduler.OfficePowerpointJobs;

[DisallowConcurrentExecution]
public class OfficePowerpointProcessingJob : IJob
{
    private readonly IConfigurationUpdater _cfgConfigurationUpdater;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly IElasticUtilities _elasticUtilities;
    private readonly IMemoryCache _memoryCache;

    public OfficePowerpointProcessingJob(IConfigurationUpdater configurationUpdater,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService, IMemoryCache memoryCache,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _cfgConfigurationUpdater = configurationUpdater;
        _actorSystem = actorSystem;
        _memoryCache = memoryCache;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
    }

    [Time]
    public async Task Execute(IJobExecutionContext context)
    {
        var logger = LoggingFactoryBuilder.Build<OfficePowerpointProcessingJob>();
        var cfg = await _cfgConfigurationUpdater.ReadConfigurationAsync();

        var statisticUtilities = StatisticUtilitiesProxy.PowerpointStatisticUtility(
            TypedDirectoryPathString.New(cfg.StatisticsDirectory),
            new StatisticModelPowerpoint().StatisticFileName);
        var comparerModel = new ComparerModelPowerpoint(cfg.ComparerDirectory);
        var jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetPowerpointJobStateMemoryCache(_memoryCache);
        jobStateMemoryCache.RemoveCacheEntry();


        var configEntry = cfg.Processing[nameof(PowerpointElasticDocument)];
        var cacheEntryOpt = jobStateMemoryCache.CacheEntry(new MemoryCacheModelPowerpointCleanup());
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
                "skip processing of powerpoint documents because the scheduler is inactive per config");
        }
        else
        {
            logger.LogInformation("start job");
            var indexName =
                _elasticUtilities.CreateIndexName(cfg.IndexName, configEntry.IndexSuffix);

            await _elasticUtilities.CheckAndCreateElasticIndex<PowerpointElasticDocument>(indexName);

            logger.LogInformation("start crunching and indexing some powerpoint documents");

            if (!Directory.Exists(cfg.ScanPath))
            {
                logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists. skip working", cfg.ScanPath);
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
                        .ProcessPowerpointDocumentAsync(configEntry, cfg,
                            statisticUtilities, logger)
                        .FilterExistingUnchangedAsync(configEntry, comparerModel)
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .WithMaybeFilter()
                        .CountFilteredDocs(statisticUtilities)
                        .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                            indexName)
                        .RunIgnoreAsync(_actorSystem.Materializer());

                    logger.LogInformation("finished processing powerpoint documents");

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
                    statisticUtilities.AddJobStatisticToDatabase(
                        jobStatistic);
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

public static class PowerpointProcessingHelper
{
    public static Source<Option<PowerpointElasticDocument>, NotUsed> ProcessPowerpointDocumentAsync(
        this Source<string, NotUsed> source,
        SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
        StatisticUtilities<StatisticModelPowerpoint> statisticUtilities, ILogger logger)
    {
        return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
            f => ProcessPowerpointDocument(f, configurationObject, statisticUtilities, logger));
    }

    private static readonly Lst<(string, string)> ToReplaced = List(
        (@"\r\n?|\n", ""),
        ("[ ]{2,}", " ")
    );

    private static async Task<Option<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelPowerpoint> statisticUtilities,
        ILogger logger)
    {
        try
        {
            (Option<PresentationPart> PresentationPartOpt, PackageProperties FInfo, PresentationDocument Document)
                docTuple = await Task.Run(
                    () =>
                    {
                        var wdOpt = PresentationDocument.Open(currentFile, false);
                        Option<PresentationPart> presentationPartOpt = wdOpt.PresentationPart!;
                        var fInfo = wdOpt.PackageProperties;
                        return (presentationPartOpt, fInfo, wdOpt);
                    });


            if (docTuple.PresentationPartOpt.IsNone)
                return Option<PowerpointElasticDocument>.None;

            var presentationPart = docTuple.PresentationPartOpt.ValueUnsafe();
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
            const string contentType = "pptx";
            var lastPrinted = docTuple.FInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = docTuple.FInfo.LastModifiedBy.IfNull(string.Empty);
            var uriPath = currentFile
                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                .Replace(@"\", "/");

            var id = await StaticHelpers.CreateHashString(TypedHashedInputString.New(currentFile));
            var slideCount = presentationPart
                .SlideParts
                .Count();

            static IEnumerable<OfficeDocumentComment>
                CommentArray(PresentationPart presentationPart) =>
                CommentsFromDocument(presentationPart.SlideParts);

            var commentsArray = CommentArray(presentationPart).ToArray();

            var contentTask = await presentationPart.Elements().ContentStringAsync();
            docTuple.Document.Close();
            docTuple.Document.Dispose();

            var contentString = contentTask
                .ReplaceSpecialStrings(ToReplaced);

            var toHash = new ElementsToHash(category, created, contentString, creator,
                description, identifier, keywords, language, modified, revision,
                subject, title, version, contentStatus, contentType, lastPrinted,
                lastModifiedBy);

            var elementsHash = await (
                StaticHelpers.ListElementsToHash(toHash), commentsArray).ContentHashStringAsync();

            var tempVal =
                await commentsArray.StringFromCommentsArray()
                    .GenerateTextToSuggestAsync(TypedContentString.New(contentString));

            static CompletionField CompletionField(TypedSuggestString suggestString) =>
                suggestString
                    .GenerateSearchAsYouTypeArray()
                    .WrapCompletionField();


            var returnValue = new PowerpointElasticDocument
            {
                Category = category,
                CompletionContent = CompletionField(tempVal),
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
                SlideCount = slideCount,
                Comments = commentsArray
            };

            return returnValue;
        }
        catch (Exception e)
        {
            logger.LogError(e, "an error while creating a indexing object at <{CurrentFile}>", currentFile);
            statisticUtilities.AddToFailedDocuments();
            return await Task.FromResult(Option<PowerpointElasticDocument>.None);
        }
    }


    private static IEnumerable<OfficeDocumentComment>
        ConvertToOfficeDocumentComment(this CommentList comments) =>
        comments.Map(comment => OfficeDocumentComment((Comment) comment));

    private static OfficeDocumentComment OfficeDocumentComment(Comment comment) =>
        new()
        {
            Comment = comment.Text.ResolveNullable(string.Empty, (v, _) => v.Text),
            Date = comment.DateTime.ResolveNullable(new DateTime(1970, 1, 1), (v, _) => v.Value)
        };

    private static IEnumerable<OfficeDocumentComment>
        CommentsFromDocument(this IEnumerable<SlidePart> slideParts) => slideParts
        .Map(part => part
            .SlideCommentsPart
            .ResolveNullable(Array.Empty<OfficeDocumentComment>(),
                (v, _) => v.CommentList.ConvertToOfficeDocumentComment().ToArray())
        )
        .Flatten();

    private static IEnumerable<OpenXmlElement> Elements(this PresentationPart presentationPart)
    {
        return presentationPart
            .SlideParts.Map(p => p.Slide);
    }
}