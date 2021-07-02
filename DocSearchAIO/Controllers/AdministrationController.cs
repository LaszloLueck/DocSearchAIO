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
            _optionDialogService = new OptionDialogService(loggerFactory, elasticSearchService);
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
        public PartialViewResult GetAdministrationModal()
        {
            _logger.LogInformation("method getAdministrationModal called");
            return new PartialViewResult
            {
                ViewName = "AdministrationModalPartial"
            };
        }

        [Route("getSchedulerStatistics")]
        [HttpGet]
        public async Task<Dictionary<string, SchedulerStatistics>> GetSchedulerStatistics()
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
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost]
        public async Task<PartialViewResult> OptionDialog(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method getOptionsDialog called!");
            var dialogResponse = await _optionDialogService.GetOptionDialog(optionDialogRequest);
            var responseModel = new TypedPartialViewResponse<OptionDialogRequest>(dialogResponse);
            return new PartialViewResult
            {
                ViewName = "ResultPageConfigurationModalPartial",
                ViewData = responseModel.GetPartialViewResponseModel()
            };
        }

        [Route("getGenericContent")]
        [HttpGet]
        public PartialViewResult GetGenericContent()
        {
            _logger.LogInformation("method getGenericContent called");
            var genericContent = _administrationService.GetGenericContent();
            var responseModel = new TypedPartialViewResponse<AdministrationGenericModel>(genericContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationGenericContentPartial",
                ViewData = responseModel.GetPartialViewResponseModel()
            };
        }

        [Route("getSchedulerContent")]
        [HttpGet]
        public async Task<PartialViewResult> GetSchedulerContent()
        {
            _logger.LogInformation("method getSchedulerContent called");
            var schedulerContent = await _administrationService.GetSchedulerContent();
            var responseModel = new TypedPartialViewResponse<Dictionary<string, SchedulerStatistics>>(schedulerContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationSchedulerContentPartial",
                ViewData = responseModel.GetPartialViewResponseModel()
            };
        }

        [Route("getStatisticsContent")]
        [HttpGet]
        public async Task<PartialViewResult> GetStatisticsContent()
        {
            _logger.LogInformation("method getStatisticsContent called");
            var statisticContent = await _administrationService.GetStatisticsContent();
            var responseModel = new TypedPartialViewResponse<IndexStatistic>(statisticContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationStatisticsContentPartial",
                ViewData = responseModel.GetPartialViewResponseModel()
            };
        }

        [Route("getActionContent")]
        [HttpGet]
        public async Task<PartialViewResult> GetActionContent()
        {
            _logger.LogInformation("method getActionContent called");
            var actionContent = await _administrationService.GetActionContent();
            var responseModel =
                new TypedPartialViewResponse<Dictionary<string, IEnumerable<AdministrationActionSchedulerModel>>>(
                    actionContent);
            return new PartialViewResult
            {
                ViewName = "AdministrationActionContentPartial",
                ViewData = responseModel.GetPartialViewResponseModel()
            };
        }
    }
}