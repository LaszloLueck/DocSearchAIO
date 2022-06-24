using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

public class SchedulerContentEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<Dictionary<string, SchedulerStatistics>>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public SchedulerContentEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SchedulerContentEndpoint>();
        _administrationService = administrationService;
    }

    [HttpPost("/api/administration/getSchedulerContentData")]
    [SwaggerOperation(
        Summary = "receive scheduler content information",
        Description = "receive scheduler content information",
        OperationId = "20586184-B181-4433-AF8E-00782A3E4BF4",
        Tags = new[] {"Administration"}
    )]
    [ProducesResponseType(typeof(Dictionary<string, SchedulerStatistics>),200)]
    public override async Task<ActionResult<Dictionary<string, SchedulerStatistics>>> HandleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getSchedulerContentData called");
        var result = _administrationService
            .SchedulerContent()
            .ToDictionaryAsync(d => d.Item1, d => d.Item2, cancellationToken: cancellationToken);
        return await result;

    }
}