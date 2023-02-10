using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Administration.GenericContent;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Jobs;

public class StartJobEndpoint : EndpointBaseAsync.WithRequest<StartJobRequest>.WithActionResult<StartJobResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public StartJobEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = LoggingFactoryBuilder.Build<StartJobEndpoint>();
    }

    [HttpPost("/api/administration/instandStartJob")]
    [SwaggerOperation(
        Summary = "start a job",
        Description = "index all documents of type",
        OperationId = "A72AEA3E-42D8-490F-AD52-28EA3493C958",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(StartJobResult), 200)]
    public override async Task<ActionResult<StartJobResult>> HandleAsync(StartJobRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method instantStartJob called");
        StartJobResult result = await _administrationService.InstantStartJobWithJobId(request);
        return result;
    }
}