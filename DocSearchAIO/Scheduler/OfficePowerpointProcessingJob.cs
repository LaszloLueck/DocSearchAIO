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
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
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
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly StatisticUtilities<StatisticModelPowerpoint> _statisticUtilities;
        private readonly ComparerModel _comparerModel;
        private readonly JobStateMemoryCache<MemoryCacheModelPowerpoint> _jobStateMemoryCache;
        private readonly ElasticUtilities _elasticUtilities;
        private readonly EncryptionService _encryptionService;

        public OfficePowerpointProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficePowerpointProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.PowerpointStatisticUtility(loggerFactory,
                new TypedDirectoryPathString(_cfg.StatisticsDirectory),
                new StatisticModelPowerpoint().StatisticFileName);
            _comparerModel = new ComparerModelPowerpoint(loggerFactory, _cfg.ComparerDirectory);
            _encryptionService = new EncryptionService();
            _jobStateMemoryCache =
                JobStateMemoryCacheProxy.GetPowerpointJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var configEntry = _cfg.Processing[nameof(PowerpointElasticDocument)];
            await Task.Run(() =>
            {
                var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelPowerpointCleanup());
                if (!cacheEntryOpt.HasNoValue &&
                    (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                {
                    _logger.LogInformation(
                        "cannot execute scanning and processing documents, opponent job cleanup running");
                    return;
                }

                configEntry
                    .Active
                    .ProcessState(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.SchedulerGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip processing of powerpoint documents because the scheduler is inactive per config");
                        },
                        async () =>
                        {
                            _logger.LogInformation("start job");
                            var indexName =
                                _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.IndexSuffix);

                            await _elasticUtilities.CheckAndCreateElasticIndex<PowerpointElasticDocument>(indexName);

                            _logger.LogInformation("start crunching and indexing some powerpoint documents");

                            Directory
                                .Exists(_cfg.ScanPath)
                                .IfTrueFalse(_cfg.ScanPath,
                                    scanPath =>
                                    {
                                        _logger.LogWarning(
                                            "directory to scan <{ScanPath}> does not exists. skip working", scanPath);
                                    },
                                    async scanPath =>
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
                                            await new TypedFilePathString(scanPath)
                                                .CreateSource(configEntry.FileExtension)
                                                .UseExcludeFileFilter(configEntry.ExcludeFilter)
                                                .CountEntireDocs(_statisticUtilities)
                                                .ProcessPowerpointDocumentAsync(configEntry, _cfg,
                                                    _statisticUtilities, _logger, _encryptionService)
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
                                    });
                        });
            });
        }
    }

    public static class PowerpointProcessingHelper
    {
        public static Source<Maybe<PowerpointElasticDocument>, NotUsed> ProcessPowerpointDocumentAsync(
            this Source<string, NotUsed> source,
            SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
            StatisticUtilities<StatisticModelPowerpoint> statisticUtilities, ILogger logger, EncryptionService encryptionService)
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                f => ProcessPowerpointDocument(f, configurationObject, statisticUtilities, logger, encryptionService));
        }

        private static async Task<Maybe<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
            ConfigurationObject configurationObject, StatisticUtilities<StatisticModelPowerpoint> statisticUtilities,
            ILogger logger, EncryptionService encryptionService)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var wdOpt = PresentationDocument.Open(currentFile, false);
                    return await wdOpt
                        .PresentationPart
                        .ResolveNullable(Task.Run(() => Maybe<PowerpointElasticDocument>.None), async (wd, _) =>
                        {
                            var fInfo = wdOpt.PackageProperties;
                            var category = fInfo.Category.ResolveNullable(string.Empty, (v, _) => v);
                            var created = fInfo.Created.ResolveNullable(new DateTime(1970, 1, 1), (v, a) => v ?? a);
                            var creator = fInfo.Creator.ResolveNullable(string.Empty, (v, _) => v);
                            var description = fInfo.Description.ResolveNullable(string.Empty, (v, _) => v);
                            var identifier = fInfo.Identifier.ResolveNullable(string.Empty, (v, _) => v);
                            var keywords = fInfo.Keywords.ResolveNullable(string.Empty, (v, _) => v);
                            var language = fInfo.Language.ResolveNullable(string.Empty, (v, _) => v);
                            var modified = fInfo.Modified.ResolveNullable(new DateTime(1970, 1, 1), (v, a) => v ?? a);
                            var revision = fInfo.Revision.ResolveNullable(string.Empty, (v, _) => v);
                            var subject = fInfo.Subject.ResolveNullable(string.Empty, (v, _) => v);
                            var title = fInfo.Title.ResolveNullable(string.Empty, (v, _) => v);
                            var version = fInfo.Version.ResolveNullable(string.Empty, (v, _) => v);
                            var contentStatus = fInfo.ContentStatus.ResolveNullable(string.Empty, (v, _) => v);
                            const string contentType = "pptx";
                            var lastPrinted = fInfo.LastPrinted.ResolveNullable(new DateTime(1970, 1, 1), (v, a) => v ?? a);
                            var lastModifiedBy = fInfo.LastModifiedBy.ResolveNullable(string.Empty, (v, _) => v);
                            var uriPath = currentFile
                                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                                .Replace(@"\", "/");

                            var id = await StaticHelpers.CreateHashString(new TypedHashedInputString(currentFile), encryptionService);
                            var slideCount = wd
                                .SlideParts
                                .Count();

                            static IEnumerable<OfficeDocumentComment>
                                CommentArray(PresentationPart presentationPart) =>
                                CommentsFromDocument(presentationPart.SlideParts);

                            var commentsArray = CommentArray(wd).ToArray();

                            var toReplaced = new List<(string, string)>()
                            {
                                (@"\r\n?|\n",""),
                                ("[ ]{2,}", " ")
                            };

                            var contentString = wd
                                .Elements()
                                .ContentString()
                                .ReplaceSpecialStrings(toReplaced);

                            var elementsHash = await (
                                StaticHelpers.ListElementsToHash(category, created, contentString, creator,
                                    description, identifier, keywords, language, modified, revision,
                                    subject, title, version, contentStatus, contentType, lastPrinted,
                                    lastModifiedBy), commentsArray).ContentHashString(encryptionService);

                            static CompletionField GetCompletionField(IEnumerable<OfficeDocumentComment> commentsArray, string contentString) =>
                                commentsArray
                                    .StringFromCommentsArray()
                                    .GenerateTextToSuggest(new TypedContentString(contentString))
                                    .GenerateSearchAsYouTypeArray()
                                    .WrapCompletionField();


                            var returnValue = new PowerpointElasticDocument
                            {
                                Category = category,
                                CompletionContent = GetCompletionField(commentsArray, contentString),
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

                            return Maybe<PowerpointElasticDocument>.From(returnValue);
                        }, t =>
                        {
                            logger.LogWarning(
                                "cannot process the base document of file {CurrentFile}, because it is null", currentFile);
                            return t;
                        });
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "an error while creating a indexing object at <{CurrentFile}>", currentFile);
                statisticUtilities.AddToFailedDocuments();
                return await Task.Run(() => Maybe<PowerpointElasticDocument>.None);
            }
        }


        private static IEnumerable<OfficeDocumentComment>
            ConvertToOfficeDocumentComment(this CommentList comments) =>
            comments.Select(comment => OfficeDocumentComment((Comment)comment));

        private static OfficeDocumentComment OfficeDocumentComment(Comment comment) =>
            new()
            {
                Comment = comment.Text.ResolveNullable(string.Empty, (v, _) => v.Text),
                Date = comment.DateTime.ResolveNullable(new DateTime(1970, 1, 1), (v, _) => v.Value)
            };

        private static IEnumerable<OfficeDocumentComment> CommentsFromDocument(this IEnumerable<SlidePart> slideParts) => slideParts
            .Select(part => part
                .SlideCommentsPart
                .ResolveNullable(Array.Empty<OfficeDocumentComment>(), (v, _) => v.CommentList.ConvertToOfficeDocumentComment().ToArray())
            )
            .SelectMany(p => p);

        private static IEnumerable<OpenXmlElement> Elements(this PresentationPart presentationPart)
        {
            return presentationPart
                .SlideParts.Select(p => p.Slide);
        }
    }
}