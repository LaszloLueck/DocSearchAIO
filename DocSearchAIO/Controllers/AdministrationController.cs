using System.Collections.Generic;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

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
        public AdministrationController(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer, IConfiguration configuration, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<AdministrationController>();
            _administrationService = new AdministrationService(loggerFactory, viewToStringRenderer, configuration);
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _optionDialogService = new OptionDialogService(loggerFactory, viewToStringRenderer, elasticClient);
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
        
    }
}