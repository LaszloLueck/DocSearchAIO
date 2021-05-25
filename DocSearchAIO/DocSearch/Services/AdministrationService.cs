using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
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

        public AdministrationService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            _loggerFactory = loggerFactory;
            _viewToStringRenderer = viewToStringRenderer;
            var cfgTmp = new ConfigurationObject();
            _configuration = configuration;
            configuration.GetSection("configurationObject").Bind(cfgTmp);
            _configurationObject = cfgTmp;
        }

        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            var content = await _viewToStringRenderer.Render("AdministrationModalPartial", new { });
            return new AdministrationModalResponse() { Content = content, ElementName = "#adminModal" };
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
            var adminGenModel = new AdministrationGenericModel();
            adminGenModel.ScanPath = _configurationObject.ScanPath;
            adminGenModel.ElasticEndpoints = _configurationObject.ElasticEndpoints;
            adminGenModel.IndexName = _configurationObject.IndexName;
            adminGenModel.SchedulerName = _configurationObject.SchedulerName;
            adminGenModel.SchedulerId = _configurationObject.SchedulerId;
            adminGenModel.ActorSystemName = _configurationObject.ActorSystemName;
            
            var content = await _viewToStringRenderer.Render("AdministrationGenericContentPartial", adminGenModel);
            return content;
        }

        public async Task<string> GetSchedulerContent()
        {
            var schedulerStatisticsService = new SchedulerStatisticsService(_loggerFactory, _configuration);

            var schedulerStatistics = await schedulerStatisticsService.GetSchedulerStatistics();
            var content = await _viewToStringRenderer.Render("AdministrationSchedulerContentPartial", schedulerStatistics);
            return content;
        }

        public async Task<string> GetStatisticsContent()
        {
            
            
            
            var content = await _viewToStringRenderer.Render("AdministrationStatisticsContentPartial", new { });
            return content;
        }

        public async Task<string> GetActionContent()
        {
            var content = await _viewToStringRenderer.Render("AdministrationActionContentPartial", new { });
            return content;
        }

    }
}