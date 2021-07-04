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
using DocSearchAIO.Utilities;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
                new TypedDirectoryPathString(_cfg.StatisticsDirectory), new StatisticModelPowerpoint().GetStatisticFileName);
            _comparerModel = new ComparerModelPowerpoint(loggerFactory, _cfg.ComparerDirectory);
            _jobStateMemoryCache =
                JobStateMemoryCacheProxy.GetPowerpointJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var configEntry = _cfg.Processing[nameof(PowerpointElasticDocument)];
            await Task.Run(() =>
            {
                var cacheEntryOpt = _jobStateMemoryCache.GetCacheEntry(new MemoryCacheModelPowerpointCleanup());
                if (!cacheEntryOpt.HasNoValue &&
                    (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                {
                    _logger.LogInformation("cannot execute scanning and processing documents, opponent job cleanup running");
                    return;
                }
                
                configEntry
                    .Active
                    .IfTrueFalse(
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
                                .IfTrueFalse((_cfg.ScanPath, _cfg.ScanPath),
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
                                            jobStatistic.EntireDocCount = _statisticUtilities.GetEntireDocumentsCount();
                                            jobStatistic.ProcessingError =
                                                _statisticUtilities.GetFailedDocumentsCount();
                                            jobStatistic.IndexedDocCount =
                                                _statisticUtilities.GetChangedDocumentsCount();
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
            StatisticUtilities<StatisticModelPowerpoint> statisticUtilities, ILogger logger)
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                f => ProcessPowerpointDocument(f, configurationObject, statisticUtilities, logger));
        }

        private static async Task<Maybe<PowerpointElasticDocument>> ProcessPowerpointDocument(string currentFile,
            ConfigurationObject configurationObject, StatisticUtilities<StatisticModelPowerpoint> statisticUtilities,
            ILogger logger)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var wdOpt = PresentationDocument
                        .Open(currentFile, false)
                        .MaybeValue();

                    return await wdOpt.Match(
                        async wd =>
                        {
                            var fInfo = wd.PackageProperties;
                            var category = fInfo.Category.ValueOr("");
                            var created =
                                new GenericSourceNullable<DateTime>(fInfo.Created).ValueOrDefault(new DateTime(1970,
                                    1,
                                    1));
                            var creator = fInfo.Creator.ValueOr("");
                            var description = fInfo.Description.ValueOr("");
                            var identifier = fInfo.Identifier.ValueOr("");
                            var keywords = fInfo.Keywords.ValueOr("");
                            var language = fInfo.Language.ValueOr("");
                            var modified =
                                new GenericSourceNullable<DateTime>(fInfo.Modified).ValueOrDefault(new DateTime(
                                    1970, 1,
                                    1));
                            var revision = fInfo.Revision.ValueOr("");
                            var subject = fInfo.Subject.ValueOr("");
                            var title = fInfo.Title.ValueOr("");
                            var version = fInfo.Version.ValueOr("");
                            var contentStatus = fInfo.ContentStatus.ValueOr("");
                            const string contentType = "pptx";
                            var lastPrinted =
                                new GenericSourceNullable<DateTime>(fInfo.LastPrinted).ValueOrDefault(new DateTime(
                                    1970,
                                    1, 1));
                            var lastModifiedBy = fInfo.LastModifiedBy.ValueOr("");
                            var uriPath = currentFile
                                .Replace(configurationObject.ScanPath, configurationObject.UriReplacement)
                                .Replace(@"\", "/");

                            var id = await StaticHelpers.CreateMd5HashString(new TypedMd5InputString(currentFile));
                            var slideCount = wd
                                .PresentationPart
                                .MaybeValue()
                                .Match(
                                    part => part.SlideParts.Count(),
                                    () => 0);

                            static IEnumerable<OfficeDocumentComment>
                                GetCommentArray(PresentationPart presentationPart) =>
                                presentationPart?
                                    .SlideParts
                                    .GetCommentsFromDocument();

                            var commentsArray = GetCommentArray(wd.PresentationPart).ToArray();

                            var toReplaced = new List<(string, string)>();

                            var contentString = wd
                                .PresentationPart
                                .GetElements()
                                .GetContentString()
                                .ReplaceSpecialStrings(toReplaced);

                            var elementsHash = await (
                                StaticHelpers.GetListElementsToHash(category, created, contentString, creator,
                                    description, identifier, keywords, language, modified, revision,
                                    subject, title, version, contentStatus, contentType, lastPrinted,
                                    lastModifiedBy), commentsArray).GetContentHashString();

                            var completionField = commentsArray
                                .GetStringFromCommentsArray()
                                .GenerateTextToSuggest(new TypedContentString(contentString))
                                .GenerateSearchAsYouTypeArray()
                                .WrapCompletionField();

                            var returnValue = new PowerpointElasticDocument
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
                                SlideCount = slideCount,
                                Comments = commentsArray
                            };

                            return Maybe<PowerpointElasticDocument>.From(returnValue);
                        },
                        async () =>
                        {
                            logger.LogWarning(
                                "cannot process the base document of file {CurrentFile}, because it is null",
                                currentFile);
                            return await Task.Run(() => Maybe<PowerpointElasticDocument>.None);
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
            comments.Select(comment => GetOfficeDocumentComment((Comment) comment));

        private static OfficeDocumentComment GetOfficeDocumentComment(Comment comment) =>
            new()
            {
                Comment = comment.Text?.Text,
                Date = new GenericSourceNullable<DateTime>(comment.DateTime).ValueOrDefault(
                    new DateTime(1970, 1, 1))
            };

        private static IEnumerable<OfficeDocumentComment>
            GetCommentsFromDocument(this IEnumerable<SlidePart> slideParts) =>
            slideParts
                .Select(part =>
                {
                    return part
                        .SlideCommentsPart
                        .MaybeValue()
                        .Match(
                            values => ConvertToOfficeDocumentComment(values.CommentList),
                            Array.Empty<OfficeDocumentComment>);
                })
                .SelectMany(p => p);

        private static IEnumerable<OpenXmlElement> GetElements(this PresentationPart presentationPart)
        {
            if (presentationPart?.SlideParts is null)
                return ArraySegment<OpenXmlElement>.Empty;

            return presentationPart
                .SlideParts.Select(p => p.Slide);
        }
    }
}