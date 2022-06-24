using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.GenericContent;

public class GetGenericConfigDataEndpoint : EndpointBaseSync.WithoutRequest.WithActionResult<AdministrationGenericRequest>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public GetGenericConfigDataEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = loggerFactory.CreateLogger<GetGenericConfigDataEndpoint>();
    }

    [HttpGet("/api/administration/getGenericContent")]
    [SwaggerOperation(
        Summary = "send generic configuration data to frontend",
        Description = "method to send generic configuration data to frontend",
        OperationId = "ADF854BA-1FE2-4FE8-A1A3-8FE8921F0A8D",
        Tags = new[] {"Administration"}
    )]
    public override ActionResult<AdministrationGenericRequest> Handle()
    {
        _logger.LogInformation("method getGenericContentData called");
        return _administrationService.GenericContent();
    }
}