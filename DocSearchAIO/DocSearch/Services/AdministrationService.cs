using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;
using JobState = DocSearchAIO.Scheduler.JobState;
using ProcessorBase = DocSearchAIO.Classes.ProcessorBase;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationService
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _configurationObject;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerStatisticsService _schedulerStatisticsService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ElasticUtilities _elasticUtilities;
        private readonly MemoryCacheModelProxy _memoryCacheModelProxy;

        public AdministrationService(ILoggerFactory loggerFactory,
            IConfiguration configuration, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            var cfgTmp = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
            _elasticSearchService = elasticSearchService;
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _memoryCacheModelProxy = new MemoryCacheModelProxy(loggerFactory, memoryCache);
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
        }

        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                    var result = await ConfigurationTuple(_configurationObject)
                        .Where(tpl => tpl.TriggerName == triggerKey.Name)
                        .TryFirst()
                        .Match(
                            async currentSelected =>
                            {
                                _logger.LogInformation("pause trigger for {TriggerName} :: {TriggerKey}",
                                    currentSelected.Key, triggerKey.Name);
                                switch (currentSelected.Item3)
                                {
                                    case "processing":
                                        _configurationObject.Processing[currentSelected.Key].Active = false;
                                        break;
                                    case "cleanup":
                                        _configurationObject.Cleanup[currentSelected.Key].Active = false;
                                        break;
                                }

                                await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Paused;
                            },
                            async () => await Task.Run(() => false)
                        );
                    await scheduler.PauseTrigger(triggerKey);
                    return result;
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
        }

        private static readonly
            Func<ConfigurationObject, IEnumerable<(string Key, string TriggerName, string SchedulerType)>>
            ConfigurationTuple =
                configurationObject =>
                {
                    var processingTuples =
                        configurationObject.Processing.SelectKv((key, value) => (key, value.TriggerName, "processing"));
                    var cleanupTuples =
                        configurationObject.Cleanup.SelectKv((key, value) => (key, value.TriggerName, "cleanup"));
                    return processingTuples.Concat(cleanupTuples);
                };

        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                    var result = await ConfigurationTuple(_configurationObject)
                        .Where(tpl => tpl.TriggerName == triggerKey.Name)
                        .TryFirst()
                        .Match(
                            async currentSelected =>
                            {
                                _logger.LogInformation("resume trigger for {TriggerName} :: {TriggerKey}",
                                    currentSelected.Key, triggerKey.Name);
                                switch (currentSelected.SchedulerType)
                                {
                                    case "processing":
                                        _configurationObject.Processing[currentSelected.Key].Active = true;
                                        break;
                                    case "cleanup":
                                        _configurationObject.Cleanup[currentSelected.Key].Active = true;
                                        break;
                                }

                                await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
                            },
                            async () => await Task.Run(() => false)
                        );
                    await scheduler.ResumeTrigger(triggerKey);
                    return result;
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
        }

        public async Task<bool> InstantStartJobWithJobId(JobStatusRequest jobStatusRequest)
        {
            var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    _logger.LogInformation("start job for job {Job} in group {Group}", jobStatusRequest.JobName,
                        jobStatusRequest.GroupId);
                    var jobKey = new JobKey(jobStatusRequest.JobName, jobStatusRequest.GroupId);
                    await scheduler.TriggerJob(jobKey);
                    return true;
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
        }

        public async Task<bool> SetAdministrationGenericContent(AdministrationGenericRequest request)
        {
            try
            {
                _configurationObject.ElasticEndpoints = request.ElasticEndpoints;
                _configurationObject.SchedulerGroupName = request.ProcessorGroupName;
                _configurationObject.IndexName = request.IndexName;
                _configurationObject.ScanPath = request.ScanPath;
                _configurationObject.SchedulerId = request.SchedulerId;
                _configurationObject.SchedulerName = request.SchedulerName;
                _configurationObject.UriReplacement = request.UriReplacement;
                _configurationObject.ActorSystemName = request.ActorSystemName;
                _configurationObject.ComparerDirectory = request.ComparerDirectory;
                _configurationObject.CleanupGroupName = request.CleanupGroupName;

                _configurationObject.Processing = request
                    .ProcessorConfigurations
                    .SelectKv((key, value) => new KeyValuePair<string, SchedulerEntry>(key, new SchedulerEntry
                    {
                        ExcludeFilter = value.ExcludeFilter,
                        FileExtension = value.FileExtension,
                        IndexSuffix = value.IndexSuffix,
                        JobName = value.JobName,
                        Parallelism = value.Parallelism,
                        RunsEvery = value.RunsEvery,
                        StartDelay = value.StartDelay,
                        TriggerName = value.TriggerName
                    }))
                    .ToDictionary();

                _configurationObject.Cleanup = request
                    .CleanupConfigurations
                    .SelectKv((key, value) =>
                        new KeyValuePair<string, CleanUpEntry>(key, new CleanUpEntry
                        {
                            ForComparerName = value.ForComparer,
                            ForIndexSuffix = value.ForIndexSuffix,
                            JobName = value.JobName,
                            Parallelism = value.Parallelism,
                            RunsEvery = value.RunsEvery,
                            StartDelay = value.StartDelay,
                            TriggerName = value.TriggerName
                        }))
                    .ToDictionary();

                await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                _logger.LogInformation("configuration successfully updated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "an error while updating the configuration occured");
                return false;
            }
        }

        private Maybe<ComparerModel> ComparerBaseFromParameter(string parameter)
        {
            try
            {
                var comparerBase = parameter switch
                {
                    nameof(WordElasticDocument) =>
                        new ComparerModelWord(_loggerFactory, _configurationObject.ComparerDirectory) as ComparerModel,
                    nameof(PowerpointElasticDocument) =>
                        new ComparerModelPowerpoint(_loggerFactory, _configurationObject.ComparerDirectory),
                    nameof(PdfElasticDocument) =>
                        new ComparerModelPdf(_loggerFactory, _configurationObject.ComparerDirectory),
                    nameof(ExcelElasticDocument) =>
                        new ComparerModelExcel(_loggerFactory, _configurationObject.ComparerDirectory),
                    _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter,
                        $"cannot cast from parameter {parameter}")
                };
                return Maybe<ComparerModel>.From(comparerBase);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "an error while converting a base parameter occured");
                return Maybe<ComparerModel>.None;
            }
        }

        public async Task<bool> DeleteIndexAndStartJob(JobStatusRequest jobStatusRequest)
        {
            var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
            await schedulerOpt.Match(
                async scheduler =>
                {
                    var boolReturn = await _configurationObject
                        .Processing
                        .Where(d => d.Value.JobName == jobStatusRequest.JobName)
                        .TryFirst()
                        .Match(
                            async (key, value) =>
                            {
                                var indexName = _elasticUtilities.CreateIndexName(_configurationObject.IndexName,
                                    value.IndexSuffix);

                                _logger.LogInformation("remove index {IndexName}", indexName);
                                await _elasticSearchService.DeleteIndexAsync(indexName);

                                _logger.LogInformation("remove comparer file for key {Key}", key);
                                ComparerBaseFromParameter(key)
                                    .Match(
                                        comparerBase => comparerBase.CleanDictionaryAndRemoveComparerFile(),
                                        () => _logger.LogWarning(
                                            "cannot determine correct comparer base from key {Key}", key));

                                _logger.LogInformation("trigger job for name {JobName}", jobStatusRequest.JobName);
                                var jobKey = new JobKey(jobStatusRequest.JobName, jobStatusRequest.GroupId);
                                await scheduler.TriggerJob(jobKey);
                                return await Task.Run(() => true);
                            },
                            async () =>
                            {
                                _logger.LogWarning("cannot remove elastic index");
                                return await Task.Run(() => false);
                            });
                    return boolReturn;
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
            return true;
        }

        public async Task<string> TriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                    return (await scheduler.GetTriggerState(triggerKey)).ToString();
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => string.Empty);
                });
        }

        public AdministrationGenericRequest GenericContent()
        {
            var processSubTypes = StaticHelpers.SubtypesOfType<ElasticDocument>();
            var cleanupSubTypes = StaticHelpers.SubtypesOfType<CleanupDocument>();

            var adminGenModel = new AdministrationGenericRequest
            {
                ScanPath = _configurationObject.ScanPath,
                ElasticEndpoints = _configurationObject.ElasticEndpoints,
                IndexName = _configurationObject.IndexName,
                SchedulerName = _configurationObject.SchedulerName,
                SchedulerId = _configurationObject.SchedulerId,
                ActorSystemName = _configurationObject.ActorSystemName,
                ProcessorGroupName = _configurationObject.SchedulerGroupName,
                CleanupGroupName = _configurationObject.CleanupGroupName,
                UriReplacement = _configurationObject.UriReplacement,
                ComparerDirectory = _configurationObject.ComparerDirectory,
                StatisticsDirectory = _configurationObject.StatisticsDirectory,
                ProcessorConfigurations = _configurationObject
                    .Processing
                    .Where(d => processSubTypes.Select(st => st.Name).Contains(d.Key))
                    .SelectKv((key, value) => new KeyValuePair<string, ProcessorConfiguration>(key,
                        new ProcessorConfiguration
                        {
                            ExcludeFilter = value.ExcludeFilter,
                            FileExtension = value.FileExtension,
                            IndexSuffix = value.IndexSuffix,
                            JobName = value.JobName,
                            Parallelism = value.Parallelism,
                            RunsEvery = value.RunsEvery,
                            StartDelay = value.StartDelay,
                            TriggerName = value.TriggerName
                        }))
                    .ToDictionary(),
                CleanupConfigurations = _configurationObject
                    .Cleanup
                    .Where(d => cleanupSubTypes.Select(st => st.Name).Contains(d.Key))
                    .SelectKv((key, value) => new KeyValuePair<string, CleanupConfiguration>(key,
                        new CleanupConfiguration
                        {
                            ForComparer = value.ForComparerName,
                            ForIndexSuffix = value.ForIndexSuffix,
                            JobName = value.JobName,
                            Parallelism = value.Parallelism,
                            RunsEvery = value.RunsEvery,
                            StartDelay = value.StartDelay,
                            TriggerName = value.TriggerName
                        }))
                    .ToDictionary()
            };

            return adminGenModel;
        }

        public async Task<Dictionary<string, SchedulerStatistics>> SchedulerContent()
        {
            return await _schedulerStatisticsService.SchedulerStatistics();
        }

        public async Task<IndexStatistic> StatisticsContent()
        {
            async Task<GetIndexResponse> IndicesResponse(string indexName) =>
                await _elasticSearchService.IndicesWithPatternAsync($"{indexName}-*");

            async Task<IEnumerable<string>> KnownIndices(string indexName) =>
                (await IndicesResponse(indexName))
                .Indices
                .Keys
                .Select(index => index.Name);

            var indexStatsResponses =
                await (await KnownIndices(_configurationObject.IndexName))
                    .Select(async index =>
                        await _elasticSearchService.IndexStatistics(index))
                    .WhenAll();

            static RunnableStatistic
                ConvertToRunnableStatistic(ProcessingJobStatistic doc, Func<MemoryCacheModel> fn) =>
                new()
                {
                    Id = doc.Id,
                    EndJob = doc.EndJob,
                    StartJob = doc.StartJob,
                    ProcessingError = doc.ProcessingError,
                    ElapsedTimeMillis = doc.ElapsedTimeMillis,
                    EntireDocCount = doc.EntireDocCount,
                    IndexedDocCount = doc.IndexedDocCount,
                    CacheEntry = fn.Invoke().CacheEntry()
                };

            static IEnumerable<KeyValuePair<ProcessorBase, Func<StatisticModel>>> StatisticUtilities(
                ILoggerFactory loggerFactory, ConfigurationObject configurationObject) =>
                StatisticUtilitiesProxy
                    .AsIEnumerable(loggerFactory,
                        new TypedDirectoryPathString(configurationObject.StatisticsDirectory));

            static IEnumerable<KeyValuePair<ProcessorBase, Func<MemoryCacheModel>>> JobStateMemoryCaches(
                ILoggerFactory loggerFactory, IMemoryCache memoryCache) =>
                JobStateMemoryCacheProxy
                    .AsIEnumerable(loggerFactory, memoryCache);

            var runtimeStatistic = new Dictionary<string, RunnableStatistic>();
            var jobStateMemoryCaches = JobStateMemoryCaches(_loggerFactory, _memoryCache);

            StatisticUtilities(_loggerFactory, _configurationObject)
                .ForEach((processorBase, statisticModel) =>
                {
                    statisticModel
                        .Invoke()
                        .LatestJobStatisticByModel()
                        .Map(doc =>
                        {
                            jobStateMemoryCaches
                                .Where(d => d.Key.DerivedModelName ==
                                            processorBase.DerivedModelName)
                                .TryFirst()
                                .Map(jobState =>
                                {
                                    runtimeStatistic.Add(processorBase.ShortName,
                                        ConvertToRunnableStatistic(doc, jobState.Value));
                                });
                        });
                });

            static IndexStatistic ResponseModel(IEnumerable<IndicesStatsResponse> indexStatsResponses,
                Dictionary<string, RunnableStatistic> runtimeStatistic) =>
                new()
                {
                    IndexStatisticModels = indexStatsResponses.Select(index => new IndexStatisticModel
                    {
                        IndexName = index.Indices.First().Key,
                        DocCount = index.Stats.Total.Documents.Count,
                        SizeInBytes = index.Stats.Total.Store.SizeInBytes,
                        FetchTimeMs = index.Stats.Total.Search.FetchTimeInMilliseconds,
                        FetchTotal = index.Stats.Total.Search.FetchTotal,
                        QueryTimeMs = index.Stats.Total.Search.QueryTimeInMilliseconds,
                        QueryTotal = index.Stats.Total.Search.QueryTotal,
                        SuggestTimeMs = index.Stats.Total.Search.SuggestTimeInMilliseconds,
                        SuggestTotal = index.Stats.Total.Search.SuggestTotal
                    }),
                    RuntimeStatistics = runtimeStatistic
                };

            return ResponseModel(indexStatsResponses, runtimeStatistic);
        }

        private static readonly Func<SchedulerStatistics, Maybe<JobState>, AdministrationActionSchedulerModel>
            ConvertToActionModel =
                (scheduler, jobStateOpt) =>
                {
                    return new AdministrationActionSchedulerModel
                    {
                        SchedulerName = scheduler.SchedulerName,
                        Triggers = scheduler.TriggerElements.Select(trigger =>
                        {
                            var triggerModel = new AdministrationActionTriggerModel
                            {
                                TriggerName = trigger.TriggerName,
                                GroupName = trigger.GroupName,
                                JobName = trigger.JobName,
                                CurrentState = trigger.TriggerState,
                                JobState = jobStateOpt.Unwrap(JobState.Undefined)
                            };
                            return triggerModel;
                        })
                    };
                };

        private static readonly Func<MemoryCacheModelProxy, Dictionary<string, JobState>> MemoryCacheStates =
            memoryCacheModelProxy => memoryCacheModelProxy
                .GetModels()
                .SelectKv((key, value) => value
                    .Element
                    .Invoke()
                    .CacheEntry()
                    .Match(
                        el => KeyValuePair.Create(key, el.JobState),
                        () => KeyValuePair.Create(key, JobState.Undefined)
                    ))
                .ToDictionary();

        private static readonly Func<string, Dictionary<string, JobState>, Maybe<JobState>> FilterMemoryCacheState =
            (jobName, memoryCacheStates) =>
                memoryCacheStates
                    .Where((key, _) => key == jobName)
                    .SelectKv((_, v) => v)
                    .TryFirst();

        private static readonly Func<SchedulerStatistics, Dictionary<string, JobState>, Maybe<JobState>>
            CalculateJobState = (schedulerStatisticsArray, memoryCacheStates) => schedulerStatisticsArray
                .TriggerElements.Select(keyElement => FilterMemoryCacheState(keyElement.JobName, memoryCacheStates))
                .Values()
                .TryFirst();

        public async Task<Dictionary<string, IEnumerable<AdministrationActionSchedulerModel>>> ActionContent()
        {
            var memoryCacheStates = MemoryCacheStates(_memoryCacheModelProxy);
            var groupedSchedulerModels = (await _schedulerStatisticsService.SchedulerStatistics())
                .SelectKv((groupName, schedulerStatisticsArray) =>
                {
                    var state = CalculateJobState(schedulerStatisticsArray, memoryCacheStates);
                    var model = ConvertToActionModel(schedulerStatisticsArray, state);
                    return new KeyValuePair<string, IEnumerable<AdministrationActionSchedulerModel>>(groupName,
                        new[] {model});
                })
                .ToDictionary();
            return groupedSchedulerModels;
        }
    }
}