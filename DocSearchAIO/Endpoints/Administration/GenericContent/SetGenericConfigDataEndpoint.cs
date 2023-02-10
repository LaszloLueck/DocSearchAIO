using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.GenericContent;

public class SetGenericConfigDataEndpoint : EndpointBaseAsync.WithRequest<AdministrationGenericRequest>.WithActionResult<
    SetGenericContentResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public SetGenericConfigDataEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _logger = LoggingFactoryBuilder.Build<SetGenericConfigDataEndpoint>();
        _administrationService = administrationService;
    }


    [HttpPost("/api/administration/setGenericContent")]
    [SwaggerOperation(
        Summary = "receive generic configuration data",
        Description = "method to receive generic configuration data from frontend and deliver it to processing",
        OperationId = "39B92746-4510-4DA1-A516-96849365EBA4",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(SetGenericContentResult), 200)]
    public override async Task<ActionResult<SetGenericContentResult>> HandleAsync(
        [FromBody] AdministrationGenericRequest request, CancellationToken cancellationToken = new())
    {
        _logger.LogInformation("method setGenericContent called");
        SetGenericContentResult result = await _administrationService.SetAdministrationGenericContent(request);
        return result;
    }
}