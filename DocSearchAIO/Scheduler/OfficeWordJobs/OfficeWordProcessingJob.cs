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
using DocumentFormat.OpenXml.Wordprocessing;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace DocSearchAIO.Scheduler.OfficeWordJobs;

[DisallowConcurrentExecution]
public class OfficeWordProcessingJob : IJob
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _cfg;
    private readonly ActorSystem _actorSystem;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ISchedulerUtilities _schedulerUtilities;
    private readonly StatisticUtilities<StatisticModelWord> _statisticUtilities;
    private readonly ComparerModel _comparerModel;
    private readonly JobStateMemoryCache<MemoryCacheModelWord> _jobStateMemoryCache;
    private readonly IElasticUtilities _elasticUtilities;

    public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
        ActorSystem actorSystem, IElasticSearchService elasticSearchService,
        IMemoryCache memoryCache, ISchedulerUtilities schedulerUtilities, IElasticUtilities elasticUtilities)
    {
        _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
        _actorSystem = actorSystem;
        _elasticSearchService = elasticSearchService;
        _schedulerUtilities = schedulerUtilities;
        _elasticUtilities = elasticUtilities;
        _statisticUtilities = StatisticUtilitiesProxy.WordStatisticUtility(loggerFactory,
            TypedDirectoryPathString.New(_cfg.StatisticsDirectory),
            new StatisticModelWord().StatisticFileName);
        _comparerModel = new ComparerModelWord(loggerFactory, _cfg.ComparerDirectory);
        _jobStateMemoryCache = JobStateMemoryCacheProxy.GetWordJobStateMemoryCache(loggerFactory, memoryCache);
        _jobStateMemoryCache.RemoveCacheEntry();
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelWordCleanup());
        if (cacheEntryOpt.IsSome &&
            (cacheEntryOpt.IsNone || cacheEntryOpt.ValueUnsafe().JobState != JobState.Stopped))
        {
            _logger.LogInformation(
                "cannot execute scanning and processing documents, opponent job cleanup running");
            return;
        }

        var configEntry = _cfg.Processing[nameof(WordElasticDocument)];
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

            await _elasticUtilities.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

            _logger.LogInformation("start crunching and indexing some word-documents");

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
                        .ProcessWordDocumentAsync(configEntry, _cfg, _statisticUtilities, _logger)
                        .FilterExistingUnchangedAsync(configEntry, _comparerModel)
                        .GroupedWithin(50, TimeSpan.FromSeconds(10))
                        .WithMaybeFilter()
                        .CountFilteredDocs(_statisticUtilities)
                        .WriteDocumentsToIndexAsync(configEntry, _elasticSearchService,
                            indexName)
                        .RunIgnore(_actorSystem.Materializer());

                    _logger.LogInformation("finished processing word-documents");
                    sw.Stop();
                    await _elasticSearchService.FlushIndexAsync(indexName);
                    await _elasticSearchService.RefreshIndexAsync(indexName);
                    jobStatistic.EndJob = DateTime.Now;
                    jobStatistic.ElapsedTimeMillis = sw.ElapsedMilliseconds;
                    jobStatistic.EntireDocCount =
                        _statisticUtilities.EntireDocumentsCount();
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

    private static async Task<Option<WordElasticDocument>> ProcessWordDocument(string currentFile,
        ConfigurationObject configurationObject, StatisticUtilities<StatisticModelWord> statisticUtilities,
        ILogger logger)
    {
        try
        {
            var wdOpt = WordprocessingDocument.Open(currentFile, false);

            Option<MainDocumentPart> mainDocumentPartOpt = wdOpt.MainDocumentPart!;
            if (mainDocumentPartOpt.IsNone)
                return Option<WordElasticDocument>.None;

            var mainDocumentPart = mainDocumentPartOpt.ValueUnsafe();
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
            const string contentType = "docx";
            var lastPrinted = fInfo.LastPrinted.IfNone(new DateTime(1970, 1, 1));
            var lastModifiedBy = fInfo.LastModifiedBy.IfNull(string.Empty);
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

            var toReplaced = new List<(string, string)>()
            {
                (@"\r\n?|\n", ""),
                ("[ ]{2,}", " ")
            };


            var contentString = mainDocumentPart
                .Elements()
                .ContentString()
                .ReplaceSpecialStrings(toReplaced);

            OfficeDocumentComment[] commentsArray = CommentArray(mainDocumentPart).ToArray();

            var toHash = new ElementsToHash(category, created, contentString,
                creator,
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
            .ResolveNullable(System.Array.Empty<OpenXmlElement>(), (v, _) => v.ChildElements.ToArray());
    }
}