using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Administration.GenericContent;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Trigger;

public class
    ResumeTriggerEndpoint : EndpointBaseAsync.WithRequest<ResumeTriggerRequest>.WithActionResult<ResumeTriggerResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public ResumeTriggerEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = LoggingFactoryBuilder.Build<ResumeTriggerEndpoint>();
    }


    [HttpPost("/api/administration/resumeTrigger")]
    [SwaggerOperation(
        Summary = "resume a trigger",
        Description = "resume a trigger with given triggerId and groupId",
        OperationId = "F05900A1-71B7-4AE6-995A-28F04C75AD3B",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(ResumeTriggerResult), 200)]
    public override async Task<ActionResult<ResumeTriggerResult>> HandleAsync([FromBody] ResumeTriggerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method resumeTrigger called");
        ResumeTriggerResult result = await _administrationService.ResumeTriggerWithTriggerId(request);
        return result;
    }
}