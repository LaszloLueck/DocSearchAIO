using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

public class
    SchedulerStatisticsEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<
        Dictionary<string, SchedulerStatistics>>
{
    private readonly ISchedulerStatisticsService _schedulerStatisticsService;
    private readonly ILogger _logger;

    public SchedulerStatisticsEndpoint(ISchedulerStatisticsService schedulerStatisticsService,
        ILoggerFactory loggerFactory)
    {
        _schedulerStatisticsService = schedulerStatisticsService;
        _logger = loggerFactory.CreateLogger<SchedulerStatisticsEndpoint>();
    }

    [HttpGet("/api/administration/getSchedulerStatistics")]
    [SwaggerOperation(
        Summary = "receive scheduler statistics",
        Description = "receive scheduler statistics",
        OperationId = "4DBE3074-7D2F-4747-A146-1469E7273C7B",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(Dictionary<string, SchedulerStatistics>), 200)]
    public override async Task<ActionResult<Dictionary<string, SchedulerStatistics>>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getSchedulerStatistics called");
        var result = _schedulerStatisticsService.SchedulerStatistics();
        return await result.ToDictionaryAsync(d => d.key.Value, d => d.statistics, cancellationToken: cancellationToken);
    }
}