using System.Collections.Generic;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("api/administration")]
    public class AdministrationController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly AdministrationService _administrationService;
        private readonly SchedulerStatisticsService _schedulerStatisticsService;
        private readonly OptionDialogService _optionDialogService;

        public AdministrationController(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer,
            IConfiguration configuration, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<AdministrationController>();
            _administrationService = new AdministrationService(loggerFactory, viewToStringRenderer, configuration, elasticSearchService);
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _optionDialogService = new OptionDialogService(loggerFactory, viewToStringRenderer, elasticSearchService);
        }

        [Route("getAdministrationModal")]
        [HttpGet]
        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            return await _administrationService.GetAdministrationModal();
        }

        [Route("getSchedulerStatistics")]
        [HttpGet]
        public async Task<IEnumerable<SchedulerStatistics>> GetSchedulerStatistics()
        {
            return await _schedulerStatisticsService.GetSchedulerStatistics();
        }

        [Route("pauseTrigger")]
        [HttpPost]
        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            return await _administrationService.PauseTriggerWithTriggerId(triggerStateRequest);
        }

        [Route("resumeTrigger")]
        [HttpPost]
        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            return await _administrationService.ResumeTriggerWithTriggerId(triggerStateRequest);
        }

        [Route("instantStartJob")]
        [HttpPost]
        public async Task<bool> InstandStartJob(JobStatusRequest jobStatusRequest)
        {
            return await _administrationService.InstantStartJobWithJobId(jobStatusRequest);
        }
        
        [Route("getTriggerStatus")]
        [HttpPost]
        public async Task<string> GetTriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            return await _administrationService.GetTriggerStatusById(triggerStateRequest);
        }

        [Route("getOptionsDialog")]
        [HttpPost]
        public async Task<OptionDialogResponse> OptionDialog(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method called!");
            return await _optionDialogService.GetOptionDialog(optionDialogRequest);
        }

        [Route("getGenericContent")]
        [HttpGet]
        public async Task<string> GetGenericContent()
        {
            _logger.LogInformation("method called");
            return await _administrationService.GetGenericContent();
        }

        [Route("getSchedulerContent")]
        [HttpGet]
        public async Task<string> GetSchedulerContent()
        {
            _logger.LogInformation("method called");
            return await _administrationService.GetSchedulerContent();
        }

        [Route("getStatisticsContent")]
        [HttpGet]
        public async Task<string> GetStatisticsContent()
        {
            _logger.LogInformation("method called");
            return await _administrationService.GetStatisticsContent();
        }

        [Route("getActionContent")]
        [HttpGet]
        public async Task<string> GetActionContent()
        {
            _logger.LogInformation("method called");
            return await _administrationService.GetActionContent();
        }
    }
}