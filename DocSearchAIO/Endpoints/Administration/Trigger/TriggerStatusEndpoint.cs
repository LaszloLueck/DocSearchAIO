using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Administration.GenericContent;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Trigger;

public class
    TriggerStatusEndpoint : EndpointBaseAsync.WithRequest<TriggerStatusRequest>.WithActionResult<TriggerStatusResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public TriggerStatusEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = loggerFactory.CreateLogger<TriggerStatusEndpoint>();
    }

    [HttpPost("/api/administration/getTriggerStatus")]
    [SwaggerOperation(
        Summary = "get a trigger status",
        Description = "get a trigger status with given triggerId and groupId",
        OperationId = "32D4CE8B-C40D-4AAF-844C-6915D43FC6FF",
        Tags = new[] {"Administration"}
    )]
    [ProducesResponseType(typeof(TriggerStatusResult), 200)]
    public override async Task<ActionResult<TriggerStatusResult>> HandleAsync([FromBody] TriggerStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getTriggerStatus called");
        TriggerStatusResult result = await _administrationService.TriggerStatusById(request);
        return result;
    }
}