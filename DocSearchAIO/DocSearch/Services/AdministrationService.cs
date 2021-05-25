using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationService
    {
        private readonly ILogger _logger;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly ConfigurationObject _configurationObject;
        private readonly IConfiguration _configuration;
        private readonly IElasticSearchService _elasticSearchService;

        public AdministrationService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer,
            IConfiguration configuration, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            _loggerFactory = loggerFactory;
            _viewToStringRenderer = viewToStringRenderer;
            _configuration = configuration;
            var cfgTmp = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
            _elasticSearchService = elasticSearchService;
        }

        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            var content = await _viewToStringRenderer.Render("AdministrationModalPartial", new { });
            return new AdministrationModalResponse() {Content = content, ElementName = "#adminModal"};
        }

        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler(_configurationObject.SchedulerName);
            var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
            if (scheduler != null)
            {
                await scheduler.PauseTrigger(triggerKey);
                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Paused;
            }

            _logger.LogWarning($"Cannot find scheduler with name {_configurationObject.SchedulerName}");
            return false;
        }

        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler(_configurationObject.SchedulerName);
            var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
            if (scheduler != null)
            {
                await scheduler.ResumeTrigger(triggerKey);
                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
            }

            _logger.LogWarning($"Cannot find scheduler with name {_configurationObject.SchedulerName}");
            return false;
        }

        public async Task<string> GetTriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler(_configurationObject.SchedulerName);
            if (scheduler != null)
            {
                var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                return (await scheduler.GetTriggerState(triggerKey)).ToString();
            }

            _logger.LogWarning($"Cannot find scheduler with name {_configurationObject.SchedulerName}");
            return "";
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
                ActorSystemName = _configurationObject.ActorSystemName
            };

            var content = await _viewToStringRenderer.Render("AdministrationGenericContentPartial", adminGenModel);
            return content;
        }

        public async Task<string> GetSchedulerContent()
        {
            var schedulerStatisticsService = new SchedulerStatisticsService(_loggerFactory, _configuration);

            var schedulerStatistics = await schedulerStatisticsService.GetSchedulerStatistics();
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

            var models = indexStatsResponses.Select(index => new IndexStatisticModel
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
            });
            
            var content = await _viewToStringRenderer.Render("AdministrationStatisticsContentPartial", models);
            return content;
        }

        public async Task<string> GetActionContent()
        {
            var content = await _viewToStringRenderer.Render("AdministrationActionContentPartial", new { });
            return content;
        }
    }
}