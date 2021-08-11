using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
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

        public AdministrationController(ILoggerFactory loggerFactory,
            IConfiguration configuration, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<AdministrationController>();
            _administrationService =
                new AdministrationService(loggerFactory, configuration, elasticSearchService, memoryCache);
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
            _optionDialogService = new OptionDialogService(loggerFactory, elasticSearchService, configuration);
        }

        [Route("setGenericContent")]
        [HttpPost]
        public async Task<bool> SetGenericContent(AdministrationGenericRequest request)
        {
            _logger.LogInformation("method setGenericContent called");
            return await _administrationService.SetAdministrationGenericContent(request);
        }
        
        [Route("getSchedulerStatistics")]
        [HttpGet]
        public async Task<Dictionary<string, SchedulerStatistics>> SchedulerStatistics()
        {
            _logger.LogInformation("method getSchedulerStatistics called");
            return await _schedulerStatisticsService.SchedulerStatistics();
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
        public async Task<string> TriggerStatusById(TriggerStateRequest triggerStateRequest)
        {
            _logger.LogInformation("method getTriggerStatus called");
            return await _administrationService.TriggerStatusById(triggerStateRequest);
        }

        [Route("getOptionsDialogData")]
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost]
        public async Task<OptionDialogResponse> OptionDialogData(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method getOptionsDialogData called!");
            return await _optionDialogService.OptionDialog(optionDialogRequest);
        }
        
        [Route("getOptionsDialog")]
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost]
        public async Task<PartialViewResult> OptionDialog(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method getOptionsDialog called!");
            var dialogResponse = await _optionDialogService.OptionDialog(optionDialogRequest);
            var responseModel = new TypedPartialViewResponse<OptionDialogResponse>(dialogResponse);
            return new PartialViewResult
            {
                ViewName = "ResultPageConfigurationModalPartial", ViewData = responseModel.PartialViewResponseModel()
            };
        }

        [Route("getGenericContentData")]
        [HttpGet]
        public AdministrationGenericRequest GenericContentData()
        {
            _logger.LogInformation("method getGenericContentData called");
            return _administrationService.GenericContent();
        }

        [Route("getSchedulerContentData")]
        [HttpGet]
        public async Task<Dictionary<string, SchedulerStatistics>> SchedulerContentData()
        {
            _logger.LogInformation("method getSchedulerContentData called");
            return await _administrationService.SchedulerContent();
        }
        
        [Route("getSchedulerContent")]
        [HttpGet]
        public async Task<PartialViewResult> SchedulerContent()
        {
            _logger.LogInformation("method getSchedulerContent called");
            var schedulerContent = await _administrationService.SchedulerContent();
            var responseModel = new TypedPartialViewResponse<Dictionary<string, SchedulerStatistics>>(schedulerContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationSchedulerContentPartial",
                ViewData = responseModel.PartialViewResponseModel()
            };
        }

        [Route("getStatisticsContentData")]
        [HttpGet]
        public async Task<IndexStatistic> StatisticsContentData()
        {
            _logger.LogInformation("method getStatisticsContentData called");
            return await _administrationService.StatisticsContent();
        }
        
        [Route("getStatisticsContent")]
        [HttpGet]
        public async Task<PartialViewResult> StatisticsContent()
        {
            _logger.LogInformation("method getStatisticsContent called");
            var statisticContent = await _administrationService.StatisticsContent();
            var responseModel = new TypedPartialViewResponse<IndexStatistic>(statisticContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationStatisticsContentPartial",
                ViewData = responseModel.PartialViewResponseModel()
            };
        }

        [Route("getActionContentData")]
        [HttpGet]
        public async Task<Dictionary<string, IEnumerable<AdministrationActionSchedulerModel>>> ActionContentData()
        {
            _logger.LogInformation("method getActionContentData called");
            return await _administrationService.ActionContent();
        }

        [Route("getActionContent")]
        [HttpGet]
        public async Task<PartialViewResult> ActionContent()
        {
            _logger.LogInformation("method getActionContent called");
            var actionContent = await _administrationService.ActionContent();
            var responseModel =
                new TypedPartialViewResponse<Dictionary<string, IEnumerable<AdministrationActionSchedulerModel>>>(
                    actionContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationActionContentPartial",
                ViewData = responseModel.PartialViewResponseModel()
            };
        }
    }
}