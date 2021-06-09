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
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly ConfigurationObject _configurationObject;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerStatisticsService _schedulerStatisticsService;
        private readonly StatisticUtilities _statisticUtilities;

        public AdministrationService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer,
            IConfiguration configuration, IElasticSearchService elasticSearchService, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            _viewToStringRenderer = viewToStringRenderer;
            var cfgTmp = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
            _elasticSearchService = elasticSearchService;
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _statisticUtilities = new StatisticUtilities(loggerFactory, liteDatabase);
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
                        .Select(r => r.Key)
                        .TryFirst()
                        .Match(
                            async currentSelected =>
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
                    var currentSelected = _configurationObject
                        .Processing
                        .Where(tpl => tpl.Value.TriggerName == triggerKey.Name)
                        .Select(r => r.Key)
                        .First();

                    _configurationObject.Processing[currentSelected].Active = true;

                    await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                    return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
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
            var adminGenModel = new AdministrationGenericModel
            {
                ScanPath = _configurationObject.ScanPath,
                ElasticEndpoints = _configurationObject.ElasticEndpoints,
                IndexName = _configurationObject.IndexName,
                SchedulerName = _configurationObject.SchedulerName,
                SchedulerId = _configurationObject.SchedulerId,
                ActorSystemName = _configurationObject.ActorSystemName,
                GroupName = _configurationObject.GroupName,
                UriReplacement = _configurationObject.UriReplacement
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
            var indicesResponse =
                await _elasticSearchService.GetIndicesWithPatternAsync($"{_configurationObject.IndexName}-*");
            var knownIndices = indicesResponse.Indices.Keys.Select(index => index.Name);

            var indexStatsResponses =
                await Task.WhenAll(knownIndices.Select(async index =>
                    await _elasticSearchService.GetIndexStatistics(index)));

            var responseModel = new IndexStatistic
            {
                IndexStatisticModels = indexStatsResponses.Select(index => new IndexStatisticModel
                {
                    IndexName = index.Indices.ToList()[0].Key,
                    DocCount = index.Stats.Total.Documents.Count,
                    SizeInBytes = index.Stats.Total.Store.SizeInBytes,
                    FetchTimeMs = index.Stats.Total.Search.FetchTimeInMilliseconds,
                    FetchTotal = index.Stats.Total.Search.FetchTotal,
                    QueryTimeMs = index.Stats.Total.Search.QueryTimeInMilliseconds,
                    QueryTotal = index.Stats.Total.Search.QueryTotal,
                    SuggestTimeMs = index.Stats.Total.Search.SuggestTimeInMilliseconds,
                    SuggestTotal = index.Stats.Total.Search.SuggestTotal
                })
            };


            var runtimeStatistic = new Dictionary<string, RunnableStatistic>();

            _statisticUtilities
                .GetLatestJobStatisticByModel<WordElasticDocument>()
                .MaybeTrue(doc =>
                {
                    var excModel = new RunnableStatistic
                    {
                        Id = doc.Id,
                        EndJob = doc.EndJob,
                        StartJob = doc.StartJob,
                        ProcessingError = doc.ProcessingError,
                        ElapsedTimeMillis = doc.ElapsedTimeMillis,
                        EntireDocCount = doc.EntireDocCount,
                        IndexedDocCount = doc.IndexedDocCount
                    };
                    runtimeStatistic.Add("Word", excModel);
                });


            _statisticUtilities
                .GetLatestJobStatisticByModel<PowerpointElasticDocument>()
                .MaybeTrue(doc =>
                {
                    var excModel = new RunnableStatistic
                    {
                        Id = doc.Id,
                        EndJob = doc.EndJob,
                        StartJob = doc.StartJob,
                        ProcessingError = doc.ProcessingError,
                        ElapsedTimeMillis = doc.ElapsedTimeMillis,
                        EntireDocCount = doc.EntireDocCount,
                        IndexedDocCount = doc.IndexedDocCount
                    };
                    runtimeStatistic.Add("Powerpoint", excModel);
                });
            _statisticUtilities
                .GetLatestJobStatisticByModel<PdfElasticDocument>()
                .MaybeTrue(doc =>
                {
                    var excModel = new RunnableStatistic
                    {
                        Id = doc.Id,
                        EndJob = doc.EndJob,
                        StartJob = doc.StartJob,
                        ProcessingError = doc.ProcessingError,
                        ElapsedTimeMillis = doc.ElapsedTimeMillis,
                        EntireDocCount = doc.EntireDocCount,
                        IndexedDocCount = doc.IndexedDocCount
                    };
                    runtimeStatistic.Add("PDF", excModel);
                });

            responseModel.RuntimeStatistics = runtimeStatistic;

            var content = await _viewToStringRenderer.Render("AdministrationStatisticsContentPartial", responseModel);
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