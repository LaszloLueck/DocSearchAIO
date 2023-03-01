using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Init;

public class InitEndpoint : EndpointBaseAsync.WithRequest<InitRequest>.WithActionResult<InitResponseObject>
{
    private readonly ILogger _logger;
    private readonly IInitService _initService;

    public InitEndpoint(ILoggerFactory loggerFactory, IInitService initService)
    {
        _logger = LoggingFactoryBuilder.Build<InitEndpoint>();
        _initService = initService;
    }

    [HttpPost("/api/base/init")]
    [SwaggerOperation(
        Summary = "the first method the ist called from any frontend.",
        Description = "Delivers information about state of index (active or inactive) and other useful informations",
        OperationId = "12FBE57B-DE86-4A45-8F38-B4F1A8227320",
        Tags = new[] { "Init" }
    )]
    [ProducesResponseType(typeof(InitResponseObject), 200)]
    public override async Task<ActionResult<InitResponseObject>> HandleAsync([FromBody] InitRequest initRequest,
        CancellationToken cancellationToken = new())
    {
        _logger.LogInformation("calling init");
        return await _initService.Init(initRequest);
    }
}