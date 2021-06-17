using System.Collections.Generic;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
            IConfiguration configuration, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<AdministrationController>();
            _administrationService = new AdministrationService(loggerFactory, viewToStringRenderer, configuration, elasticSearchService, memoryCache);
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _optionDialogService = new OptionDialogService(loggerFactory, viewToStringRenderer, elasticSearchService);
        }

        [Route("setGenericContent")]
        [HttpPost]
        public async Task<bool> SetGenericContent(AdministrationGenericModel model)
        {
            _logger.LogInformation("method setGenericContent called");
            return await _administrationService.SetAdministrationGenericContent(model);
        }
        
        [Route("getAdministrationModal")]
        [HttpGet]
        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            _logger.LogInformation("method getAdministrationModal called");
            return await _administrationService.GetAdministrationModal();
        }

        [Route("getSchedulerStatistics")]
        [HttpGet]
        public async Task<IEnumerable<SchedulerStatistics>> GetSchedulerStatistics()
        {
            _logger.LogInformation("method getSchedulerStatistics called");
            return await _schedulerStatisticsService.GetSchedulerStatistics();
        }

        [Route("pauseTrigger")]
        [HttpPost]
        public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            _logger.LogInformation("method pauseTrigger called");
            return await _administrationService.PauseTriggerWithTriggerId(triggerStateRequest);
        }

        [Route("resumeTrigger")]
        [HttpPost]
        public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
        {
            _logger.LogInformation("method resumeTrigger called");
            return await _administrationService.ResumeTriggerWithTriggerId(triggerStateRequest);
        }

        [Route("instantStartJob")]
        [HttpPost]
        public async Task<bool> InstantStartJob(JobStatusRequest jobStatusRequest)
        {
            _logger.LogInformation("method instantStartJob called");
            return await _administrationService.InstantStartJobWithJobId(jobStatusRequest);
        }

        [Route("reindexAndStartJob")]
        [HttpPost]
        public async Task<bool> ReindexAndStartJob(JobStatusRequest jobStatusRequest)
        {
            _logger.LogInformation("method reindexAndStartJob called");
            return await _administrationService.DeleteIndexAndStartJob(jobStatusRequest);
        }
        
        [Route("getTriggerStatus")]
        [HttpPost]
        public async Task<string> GetTriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            _logger.LogInformation("method getTriggerStatus called");
            return await _administrationService.GetTriggerStatusById(triggerStateRequest);
        }

        [Route("getOptionsDialog")]
        [HttpPost]
        public async Task<OptionDialogResponse> OptionDialog(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method getOptionsDialog called!");
            return await _optionDialogService.GetOptionDialog(optionDialogRequest);
        }

        [Route("getGenericContent")]
        [HttpGet]
        public async Task<string> GetGenericContent()
        {
            _logger.LogInformation("method getGenericContent called");
            return await _administrationService.GetGenericContent();
        }

        [Route("getSchedulerContent")]
        [HttpGet]
        public async Task<string> GetSchedulerContent()
        {
            _logger.LogInformation("method getSchedulerContent called");
            return await _administrationService.GetSchedulerContent();
        }

        [Route("getStatisticsContent")]
        [HttpGet]
        public async Task<string> GetStatisticsContent()
        {
            _logger.LogInformation("method getStatisticsContent called");
            return await _administrationService.GetStatisticsContent();
        }

        [Route("getActionContent")]
        [HttpGet]
        public async Task<string> GetActionContent()
        {
            _logger.LogInformation("method getActionContent called");
            return await _administrationService.GetActionContent();
        }
    }
}