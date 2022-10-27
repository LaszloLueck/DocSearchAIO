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
using DocumentFormat.OpenXml.Presentation;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficePowerpointJobs;

[DisallowConcurrentExecution]
public class OfficePowerpointProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelPowerpoint> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelPowerpoint> _jobStateMemoryCache;
    private readonly IElasticUtilities _elasticUtilities;

    public OfficePowerpointProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService, IMemoryCache memoryCache,
        ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<OfficePowerpointProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _statisticUtilities = StatisticUtilitiesProxy.PowerpointStatisticUtility(loggerFactory,
            TypedDirectoryPathString.New(_cfg.StatisticsDirectory),
            new StatisticModelPowerpoint().StatisticFileName);
        _comparerModel = new ComparerModelPowerpoint(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache =
            JobStateMemoryCacheProxy.GetPowerpointJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var configEntry = _cfg.Processing[nameof(PowerpointElasticDocument)];
        var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelPowerpointCleanup());
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
                "skip processing of powerpoint documents because the scheduler is inactive per config");
        }
        else
        {
            _logger.LogInformation("start job");
            var indexName =
                _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);

            await _elasticUtilities.CheckAndCreateElasticIndex<PowerpointElasticDocument>(indexName);

            _logger.LogInformation("start crunching and indexing some powerpoint documents");

            if (!Directory.Exists(_cfg.ScanPath))
            {
                _logger.LogWarning(
                    "directory to scan <{ScanPath}> does not exists. skip working", _cfg.ScanPath);
            }
            else
            {
                try
                {
                    _jobStateMemoryCache.SetCacheEntry(JobState.Running);
                    var jobStatistic = new ProcessingJobStatistic
                    {
                        Id = Guid.NewGuid().ToString(),
                        StartJob = DateTime.Now
                    };

                    var sw = Stopwatch.StartNew();
                    await TypedFilePathString.New(_cfg.ScanPath)
                        .CreateSource(configEntry.FileExtension)
                        .UseExcludeFileFilter(configEntry.ExcludeFilter)
                        .CountEntireDocs(_statisticUtilities)
                        .ProcessPowerpointDocumentAsync(configEntry, _cfg,
                            _statisticUtilities, _logger)
                        .FilterExistingUnchangedAsync(configEntry, _comparerModel)
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .WithMaybeFilter()
                        .CountFilteredDocs(_statisticUtilities)
                        .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                            indexName)
                        .RunIgnore(_actorSystem.Materializer());

                    _logger.LogInformation("finished processing powerpoint documents");

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
                    _statisticUtilities.AddJobStatisticToDatabase(
                        jobStatistic);
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

    private static async Task<Option<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelPowerpoint> statisticUtilities,
        ILogger logger)
    {
        try
        {
            var wdOpt = PresentationDocument.Open(currentFile, false);

            Option<PresentationPart> presentationPartOpt = wdOpt.PresentationPart!;
            if (presentationPartOpt.IsNone)
                return Option<PowerpointElasticDocument>.None;

            var presentationPart = presentationPartOpt.ValueUnsafe();
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
            const string contentType = "pptx";
            var lastPrinted = fInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = fInfo.LastModifiedBy.IfNull(string.Empty);
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

            var toReplaced = new List<(string, string)>()
            {
                (@"\r\n?|\n", ""),
                ("[ ]{2,}", " ")
            };

            var contentTask = await presentationPart.Elements().ContentString();

            var contentString = contentTask
                .ReplaceSpecialStrings(toReplaced);

            var toHash = new ElementsToHash(category, created, contentString, creator,
                description, identifier, keywords, language, modified, revision,
                subject, title, version, contentStatus, contentType, lastPrinted,
                lastModifiedBy);

            var elementsHash = await (
                StaticHelpers.ListElementsToHash(toHash), commentsArray).ContentHashString();
            
            var tempVal =
            await commentsArray.StringFromCommentsArray().GenerateTextToSuggest(TypedContentString.New(contentString));

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
            .ResolveNullable(System.Array.Empty<OfficeDocumentComment>(),
                (v, _) => v.CommentList.ConvertToOfficeDocumentComment().ToArray())
        )
        .Flatten();

    private static IEnumerable<OpenXmlElement> Elements(this PresentationPart presentationPart)
    {
        return presentationPart
            .SlideParts.Map(p => p.Slide);
    }
}