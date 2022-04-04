using System.Net.Mime;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.Controllers;

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
        _optionDialogService = new OptionDialogService(elasticSearchService, configuration);
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
    public  IAsyncEnumerable<KeyValuePair<string, SchedulerStatistics>> SchedulerStatistics()
    {
        _logger.LogInformation("method getSchedulerStatistics called");
        return  _schedulerStatisticsService.SchedulerStatistics();
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

    [Route("getGenericContentData")]
    [HttpGet]
    public AdministrationGenericRequest GenericContentData()
    {
        _logger.LogInformation("method getGenericContentData called");
        return _administrationService.GenericContent();
    }

    [Route("getSchedulerContentData")]
    [HttpGet]
    public IAsyncEnumerable<KeyValuePair<string, SchedulerStatistics>> SchedulerContentData()
    {
        _logger.LogInformation("method getSchedulerContentData called");
        return _administrationService.SchedulerContent();
    }

    [Route("getStatisticsContentData")]
    [HttpGet]
    public async Task<IndexStatistic> StatisticsContentData()
    {
        _logger.LogInformation("method getStatisticsContentData called");
        return await _administrationService.StatisticsContent();
    }

    [Route("getActionContentData")]
    [HttpGet]
    public IAsyncEnumerable<KeyValuePair<string, IEnumerable<AdministrationActionSchedulerModel>>> ActionContentData()
    {
        _logger.LogInformation("method getActionContentData called");
        return _administrationService.ActionContent();
    }
}