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
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ComparerModel _comparerModel;
        private readonly JobStateMemoryCache<MemoryCacheModelWord> _jobStateMemoryCache;
        private readonly ElasticUtilities _elasticUtilities;

        public OfficeWordProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService,
            IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.WordStatisticUtility(loggerFactory,
                new TypedDirectoryPathString(_cfg.StatisticsDirectory),
                new StatisticModelWord().StatisticFileName);
            _comparerModel = new ComparerModelWord(loggerFactory, _cfg.ComparerDirectory);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetWordJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var cacheEntryOpt = _jobStateMemoryCache.CacheEntry(new MemoryCacheModelWordCleanup());
                if (!cacheEntryOpt.HasNoValue &&
                    (!cacheEntryOpt.HasValue || cacheEntryOpt.Value.JobState != JobState.Stopped))
                {
                    _logger.LogInformation(
                        "cannot execute scanning and processing documents, opponent job cleanup running");
                    return;
                }

                var configEntry = _cfg.Processing[nameof(WordElasticDocument)];
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
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

                            await _elasticUtilities.CheckAndCreateElasticIndex<WordElasticDocument>(indexName);

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
                                            await new TypedFilePathString(scanPath)
                                                .CreateSource(configEntry.FileExtension)
                                                .UseExcludeFileFilter(configEntry.ExcludeFilter)
                                                .CountEntireDocs(_statisticUtilities)
                                                .ProcessWordDocumentAsync(configEntry, _cfg, _statisticUtilities,
                                                    _logger)
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
                                    });
                        });
            });
        }
    }

    internal static class WordProcessingHelper
    {
        public static Source<Maybe<WordElasticDocument>, NotUsed> ProcessWordDocumentAsync(
            this Source<string, NotUsed> source,
            SchedulerEntry schedulerEntry, ConfigurationObject configurationObject,
            StatisticUtilities<StatisticModelWord> statisticUtilities, ILogger logger)
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                f => ProcessWordDocument(f, configurationObject, statisticUtilities, logger));
        }

        private static async Task<Maybe<WordElasticDocument>> ProcessWordDocument(string currentFile,
            ConfigurationObject configurationObject, StatisticUtilities<StatisticModelWord> statisticUtilities,
            ILogger logger)
        {
            try
            {
                return await Task.Run(async () =>
                {
                    var wdOpt = WordprocessingDocument.Open(currentFile, false);

                    return await wdOpt
                        .MainDocumentPart
                        .ResolveNullable(Task.Run(() => Maybe<WordElasticDocument>.None), async (mainDocumentPart, _) =>
                        {
                            var fInfo = wdOpt.PackageProperties;
                            var category = fInfo.Category.ResolveNullable(string.Empty, (v, _) => v);
                            var created = fInfo.Created.ResolveNullable(new DateTime(1970, 1, 1),
                                (value, a) => value ?? a);
                            var creator = fInfo.Creator.ResolveNullable(string.Empty, (v, _) => v);
                            var description = fInfo.Description.ResolveNullable(string.Empty, (v, _) => v);
                            var identifier = fInfo.Identifier.ResolveNullable(string.Empty, (v, _) => v);
                            var keywords = fInfo.Keywords.ResolveNullable(string.Empty, (v, _) => v);
                            var language = fInfo.Language.ResolveNullable(string.Empty, (v, _) => v);
                            var modified = fInfo.Modified.ResolveNullable(new DateTime(1970, 1, 1),
                                (value, a) => value ?? a);
                            var revision = fInfo.Revision.ResolveNullable(string.Empty, (v, _) => v);
                            var subject = fInfo.Subject.ResolveNullable(string.Empty, (v, _) => v);
                            var title = fInfo.Title.ResolveNullable(string.Empty, (v, _) => v);
                            var version = fInfo.Version.ResolveNullable(string.Empty, (v, _) => v);
                            var contentStatus = fInfo.ContentStatus.ResolveNullable(string.Empty, (v, _) => v);
                            const string contentType = "docx";
                            var lastPrinted = fInfo.LastPrinted.ResolveNullable(new DateTime(1970, 1, 1),
                                (value, a) => value ?? a);
                            var lastModifiedBy = fInfo.LastModifiedBy.ResolveNullable(string.Empty, (v, _) => v);
                            var uriPath = currentFile
                                .Replace(configurationObject.ScanPath,
                                    configurationObject.UriReplacement)
                                .Replace(@"\", "/");

                            var id = await StaticHelpers.CreateMd5HashString(
                                new TypedMd5InputString(currentFile));


                            static OfficeDocumentComment[] CommentArray(
                                MainDocumentPart mainDocumentPart)
                            {
                                var comments = mainDocumentPart
                                    .WordprocessingCommentsPart
                                    .ResolveNullable(new Comments(), (v, _) => v.Comments);

                                return comments.Select(comment =>
                                {
                                    var d = (Comment) comment;
                                    var retValue = new OfficeDocumentComment
                                    {
                                        Author = d.Author.ResolveNullable(string.Empty,
                                            (value, alternative) => value.Value ?? alternative),
                                        Comment = d.InnerText,
                                        Date = d.Date.ResolveNullable(new DateTime(1970, 1, 1),
                                            (value, _) => value.Value),
                                        Id = d.Id.ResolveNullable(string.Empty,
                                            (value, alternative) => value.Value ?? alternative),
                                        Initials = d.Initials.ResolveNullable(string.Empty,
                                            (value, alternative) => value.Value ?? alternative)
                                    };
                                    return retValue;
                                }).ToArray();
                            }

                            var toReplaced = new List<(string, string)>();


                            var contentString = mainDocumentPart
                                .Elements()
                                .ContentString()
                                .ReplaceSpecialStrings(toReplaced);

                            var commentsArray = CommentArray(mainDocumentPart);

                            var elementsHash = await (
                                    StaticHelpers.ListElementsToHash(category, created, contentString,
                                        creator,
                                        description, identifier, keywords, language, modified, revision,
                                        subject, title, version, contentStatus, contentType, lastPrinted,
                                        lastModifiedBy), commentsArray)
                                .ContentHashString();

                            var completionField = commentsArray
                                .StringFromCommentsArray()
                                .GenerateTextToSuggest(new TypedContentString(contentString))
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

                            return Maybe<WordElasticDocument>.From(returnValue);
                        }, t =>
                        {
                            logger.LogWarning("cannot process main document part of file {CurrentFile}, because it is null", currentFile);
                            return t;
                        });
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "an error while creating a indexing object");
                statisticUtilities.AddToFailedDocuments();
                return await Task.Run(() => Maybe<WordElasticDocument>.None);
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
}