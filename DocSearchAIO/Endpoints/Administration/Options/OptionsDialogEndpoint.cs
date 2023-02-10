using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Options;

public class
    OptionsDialogEndpoint : EndpointBaseAsync.WithRequest<OptionDialogRequest>.WithActionResult<OptionDialogResponse>
{
    private readonly IOptionDialogService _optionDialogService;
    private readonly ILogger _logger;

    public OptionsDialogEndpoint(IOptionDialogService optionDialogService, ILoggerFactory loggerFactory)
    {
        _optionDialogService = optionDialogService;
        _logger = LoggingFactoryBuilder.Build<OptionsDialogEndpoint>();
    }

    [HttpPost("/api/administration/getOptionsDialogData")]
    [SwaggerOperation(
        Summary = "data for options dialog",
        Description = "receive data necessary for showing in options dialog",
        OperationId = "FEFF0650-E863-4080-BBEF-FF29B2475F20",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(OptionDialogResponse), 200)]
    public override async Task<ActionResult<OptionDialogResponse>> HandleAsync(OptionDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getOptionsDialogData called!");
        return await _optionDialogService.OptionDialog(request);
    }
}