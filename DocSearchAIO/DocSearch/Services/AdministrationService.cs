using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Util.Internal;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;
using ProcessorBase = DocSearchAIO.Classes.ProcessorBase;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly ConfigurationObject _configurationObject;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerStatisticsService _schedulerStatisticsService;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMemoryCache _memoryCache;

        public AdministrationService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer,
            IConfiguration configuration, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            _viewToStringRenderer = viewToStringRenderer;
            var cfgTmp = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
            _elasticSearchService = elasticSearchService;
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _loggerFactory = loggerFactory;
            _memoryCache = memoryCache;
        }

        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            var content = await _viewToStringRenderer.Render("AdministrationModalPartial", new { });
            return new AdministrationModalResponse {Content = content, ElementName = "#adminModal"};
        }

        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                    await scheduler.PauseTrigger(triggerKey);
                    return await _configurationObject
                        .Processing
                        .Where(tpl => tpl.Value.TriggerName == triggerKey.Name)
                        .TryFirst()
                        .Match(
                            async (currentSelected, _) =>
                            {
                                _configurationObject.Processing[currentSelected].Active = false;
                                await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Paused;
                            },
                            async () => await Task.Run(() => false)
                        );
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
        }

        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
                    var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                    await scheduler.ResumeTrigger(triggerKey);
                    return await _configurationObject
                        .Processing
                        .Where(tpl => tpl.Value.TriggerName == triggerKey.Name)
                        .TryFirst()
                        .Match(
                            async (currentSelected, _) =>
                            {
                                _configurationObject.Processing[currentSelected].Active = true;
                                await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
                            },
                            async () => await Task.Run(() => false)
                        );
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
            var schedulerOpt = await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName);
            return await schedulerOpt.Match(
                async scheduler =>
                {
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

        public async Task<bool> SetAdministrationGenericContent(AdministrationGenericModel model)
        {
            try
            {
                _configurationObject.ElasticEndpoints = model.ElasticEndpoints;
                _configurationObject.GroupName = model.GroupName;
                _configurationObject.IndexName = model.IndexName;
                _configurationObject.ScanPath = model.ScanPath;
                _configurationObject.SchedulerId = model.SchedulerId;
                _configurationObject.SchedulerName = model.SchedulerName;
                _configurationObject.UriReplacement = model.UriReplacement;
                _configurationObject.ActorSystemName = model.ActorSystemName;
                _configurationObject.ComparerDirectory = model.ComparerDirectory;

                _configurationObject.Processing = model
                    .ProcessorConfigurations
                    .Select(kv =>
                        new KeyValuePair<string, SchedulerEntry>(kv.Key, new SchedulerEntry()
                        {
                            ExcludeFilter = kv.Value.ExcludeFilter,
                            FileExtension = kv.Value.FileExtension,
                            IndexSuffix = kv.Value.IndexSuffix,
                            JobName = kv.Value.JobName,
                            Parallelism = kv.Value.Parallelism,
                            RunsEvery = kv.Value.RunsEvery,
                            StartDelay = kv.Value.StartDelay,
                            TriggerName = kv.Value.TriggerName
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

        private Maybe<ComparerModel> GetComparerBaseFromParameter(string parameter)
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
            var schedulerOpt = await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName);
            await schedulerOpt.Match(
                async scheduler =>
                {
                    var t = await _configurationObject
                        .Processing
                        .Where(d => d.Value.JobName == jobStatusRequest.JobName)
                        .TryFirst()
                        .Match(
                            async (key, value) =>
                            {
                                var indexName = _schedulerUtilities.CreateIndexName(_configurationObject.IndexName,
                                    value.IndexSuffix);

                                _logger.LogInformation("remove index {IndexName}", indexName);
                                await _elasticSearchService.DeleteIndexAsync(indexName);

                                _logger.LogInformation("remove comparer file for key {Key}", key);
                                GetComparerBaseFromParameter(key)
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
                    return t;
                },
                async () =>
                {
                    _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                        _configurationObject.SchedulerName);
                    return await Task.Run(() => false);
                });
            return true;
        }

        public async Task<string> GetTriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            var schedulerOpt = await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName);
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

        public async Task<string> GetGenericContent()
        {
            var subTypes = StaticHelpers.GetSubtypesOfType<ElasticDocument>();
            var adminGenModel = new AdministrationGenericModel
            {
                ScanPath = _configurationObject.ScanPath,
                ElasticEndpoints = _configurationObject.ElasticEndpoints,
                IndexName = _configurationObject.IndexName,
                SchedulerName = _configurationObject.SchedulerName,
                SchedulerId = _configurationObject.SchedulerId,
                ActorSystemName = _configurationObject.ActorSystemName,
                GroupName = _configurationObject.GroupName,
                UriReplacement = _configurationObject.UriReplacement,
                ComparerDirectory = _configurationObject.ComparerDirectory,
                StatisticsDirectory = _configurationObject.StatisticsDirectory,
                ProcessorConfigurations = _configurationObject
                    .Processing
                    .Where(d => subTypes.Select(st => st.Name).Contains(d.Key))
                    .Select(kv =>
                        new KeyValuePair<string, AdministrationGenericModel.ProcessorConfiguration>(kv.Key,
                            new AdministrationGenericModel.ProcessorConfiguration()
                            {
                                ExcludeFilter = kv.Value.ExcludeFilter,
                                FileExtension = kv.Value.FileExtension,
                                IndexSuffix = kv.Value.IndexSuffix,
                                JobName = kv.Value.JobName,
                                Parallelism = kv.Value.Parallelism,
                                RunsEvery = kv.Value.RunsEvery,
                                StartDelay = kv.Value.StartDelay,
                                TriggerName = kv.Value.TriggerName
                            }))
                    .ToDictionary()
            };


            var content = await _viewToStringRenderer.Render("AdministrationGenericContentPartial", adminGenModel);
            return content;
        }


        public async Task<string> GetSchedulerContent()
        {
            var schedulerStatistics = await _schedulerStatisticsService.GetSchedulerStatistics();
            var content =
                await _viewToStringRenderer.Render("AdministrationSchedulerContentPartial", schedulerStatistics);
            return content;
        }

        public async Task<string> GetStatisticsContent()
        {
            async Task<GetIndexResponse> GetIndicesResponse(string indexName) =>
                await _elasticSearchService.GetIndicesWithPatternAsync($"{indexName}-*");

            async Task<IEnumerable<string>> GetKnownIndices(string indexName) =>
                (await GetIndicesResponse(indexName))
                .Indices
                .Keys
                .Select(index => index.Name);

            var indexStatsResponses =
                await Task.WhenAll((await GetKnownIndices(_configurationObject.IndexName))
                    .Select(async index =>
                        await _elasticSearchService.GetIndexStatistics(index)));

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
                    CacheEntry = fn.Invoke().GetCacheEntry()
                };

            static IEnumerable<KeyValuePair<ProcessorBase, Func<StatisticModel>>> StatisticUtilities(
                ILoggerFactory loggerFactory, ConfigurationObject configurationObject) =>
                StatisticUtilitiesProxy
                    .AsIEnumerable(loggerFactory, configurationObject.StatisticsDirectory);

            static IEnumerable<KeyValuePair<ProcessorBase, Func<MemoryCacheModel>>> GetJobStateMemoryCaches(
                ILoggerFactory loggerFactory, IMemoryCache memoryCache) =>
                JobStateMemoryCacheProxy
                    .AsIEnumerable(loggerFactory, memoryCache);

            var runtimeStatistic = new Dictionary<string, RunnableStatistic>();
            var jobStateMemoryCaches = GetJobStateMemoryCaches(_loggerFactory, _memoryCache);

            StatisticUtilities(_loggerFactory, _configurationObject)
                .ForEach((processorBase, statisticModel) =>
                {
                    statisticModel
                        .Invoke()
                        .GetLatestJobStatisticByModel()
                        .Map(doc =>
                        {
                            jobStateMemoryCaches
                                .Where(d => d.Key.GetDerivedModelName ==
                                            processorBase.GetDerivedModelName)
                                .TryFirst()
                                .Map(jobState =>
                                {
                                    runtimeStatistic.Add(processorBase.ShortName,
                                        ConvertToRunnableStatistic(doc, jobState.Value));
                                });
                        });
                });

            static IndexStatistic GetResponseModel(IEnumerable<IndicesStatsResponse> indexStatsResponses,
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

            var responseModel = GetResponseModel(indexStatsResponses, runtimeStatistic);

            var content =
                await _viewToStringRenderer.Render("AdministrationStatisticsContentPartial", responseModel);
            return content;
        }

        public async Task<string> GetActionContent()
        {
            var schedulerStatistics = await _schedulerStatisticsService.GetSchedulerStatistics();
            var schedulerModels = schedulerStatistics.Select(scheduler =>
            {
                var schedulerModel = new AdministrationActionSchedulerModel
                {
                    SchedulerName = scheduler.SchedulerName,
                    Triggers = scheduler.TriggerElements.Select(trigger =>
                    {
                        var triggerModel = new AdministrationActionTriggerModel
                        {
                            TriggerName = trigger.TriggerName,
                            GroupName = trigger.GroupName,
                            JobName = trigger.JobName,
                            CurrentState = trigger.TriggerState
                        };
                        return triggerModel;
                    })
                };
                return schedulerModel;
            });


            var content = await _viewToStringRenderer.Render("AdministrationActionContentPartial", schedulerModels);
            return content;
        }
    }
}