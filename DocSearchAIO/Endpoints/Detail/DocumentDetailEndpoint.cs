using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Detail;

public class DocumentDetailEndpoint : EndpointBaseAsync.WithRequest<DocumentDetailRequest>.WithActionResult<DocumentDetailModel>
{

    private readonly IDocumentDetailService _documentDetailService;
    private readonly ILogger _logger;

    public DocumentDetailEndpoint(IDocumentDetailService documentDetailService, ILoggerFactory loggerFactory)
    {
        _documentDetailService = documentDetailService;
        _logger = LoggingFactoryBuilder.Build<DocumentDetailEndpoint>();
    }


    [HttpPost("/api/search/documentDetailData")]
    [SwaggerOperation(
        Summary = "retrieve document detail information",
        Description = "delivers some detail information about a specific document",
        OperationId = "DF04A1F0-7DB8-464A-ACA4-B082E0164E1D",
        Tags = new[] { "DocumentDetail" }
    )]
    public override async Task<ActionResult<DocumentDetailModel>> HandleAsync([FromBody] DocumentDetailRequest request, CancellationToken cancellationToken = new())
    {
        _logger.LogInformation("get document details for {RequestId}", request.Id);
        return await _documentDetailService.DocumentDetail(request);
    }
}