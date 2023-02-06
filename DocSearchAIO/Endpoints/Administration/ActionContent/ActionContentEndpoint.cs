using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.ActionContent;

public class ActionContentEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<
    Dictionary<string, AdministrationActionSchedulerModel>>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public ActionContentEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = loggerFactory.CreateLogger<ActionContentEndpoint>();
    }

    [HttpGet("/api/administration/getActionContentData")]
    [SwaggerOperation(
        Summary = "data for action content",
        Description = "receive data for action content",
        OperationId = "9D32F270-8FA6-45D1-A28E-5652380072EF",
        Tags = new[] {"Administration"}
    )]
    [ProducesResponseType(typeof(Dictionary<string, AdministrationActionSchedulerModel>), 200)]
    public override async Task<ActionResult<Dictionary<string, AdministrationActionSchedulerModel>>>
        HandleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getActionContentData called");
        var result = await _administrationService.ActionContent();

        return result.ToDictionary(d => d.Item1.Value, d => d.Item2);
    }
}