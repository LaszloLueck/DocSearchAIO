using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Administration.GenericContent;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Trigger;

public class
    PauseTriggerEndpoint : EndpointBaseAsync.WithRequest<PauseTriggerRequest>.WithActionResult<PauseTriggerResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public PauseTriggerEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = loggerFactory.CreateLogger<PauseTriggerEndpoint>();
    }

    [HttpPost("/api/administration/pauseTrigger")]
    [SwaggerOperation(
        Summary = "pause a trigger",
        Description = "pause a trigger with given triggerId and groupId",
        OperationId = "C014AE76-1183-436C-A861-B442862770B2",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(PauseTriggerResult), 200)]
    public override async Task<ActionResult<PauseTriggerResult>> HandleAsync([FromBody] PauseTriggerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method pauseTrigger called");
        PauseTriggerResult result = await _administrationService.PauseTriggerWithTriggerId(request);
        return result;
    }
}