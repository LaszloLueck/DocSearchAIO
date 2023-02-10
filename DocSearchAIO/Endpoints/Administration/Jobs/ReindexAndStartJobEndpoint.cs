using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Administration.GenericContent;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Jobs;

public class ReindexAndStartJobEndpoint : EndpointBaseAsync.WithRequest<ReindexAndStartJobRequest>.WithActionResult<ReindexAndStartJobResult>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public ReindexAndStartJobEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = LoggingFactoryBuilder.Build<ReindexAndStartJobEndpoint>();
    }

    [HttpPost("/api/administration/reindexAndStartJob")]
    [SwaggerOperation(
        Summary = "start a job with reindexing",
        Description = "totally cleanup a search index and then index all documents of type",
        OperationId = "E26D7B0F-A6AA-4ADA-9E31-0246B93168D0",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(ReindexAndStartJobResult), 200)]
    public override async Task<ActionResult<ReindexAndStartJobResult>> HandleAsync([FromBody] ReindexAndStartJobRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method reindexAndStartJob called");
        ReindexAndStartJobResult result = await _administrationService.DeleteIndexAndStartJob(request);
        return result;
    }
}