using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;

        public AdministrationService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<AdministrationService>();
            _viewToStringRenderer = viewToStringRenderer;
        }

        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            var content = await _viewToStringRenderer.Render("AdministrationModalPartial", new { });
            return new AdministrationModalResponse() {Content = content, ElementName = "#adminModal"};
        }

        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler("QuartzScheduler");
            var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
            if (scheduler != null)
            {
                await scheduler.PauseTrigger(triggerKey);
                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Paused;
            }

            _logger.LogWarning("Cannot find scheduler with name QuartzScheduler");
            return false;
        }

        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler("QuartzScheduler");
            var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
            if (scheduler != null)
            {
                await scheduler.ResumeTrigger(triggerKey);
                return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
            }

            _logger.LogWarning("Cannot find scheduler with name QuartzScheduler");
            return false;
        }

        public async Task<string> GetTriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler("QuartzScheduler");
            if (scheduler != null)
            {
                var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                return (await scheduler.GetTriggerState(triggerKey)).ToString();
            }
            _logger.LogWarning("Cannot find scheduler with name QuartzScheduler");
            return "";
        }
    }
}