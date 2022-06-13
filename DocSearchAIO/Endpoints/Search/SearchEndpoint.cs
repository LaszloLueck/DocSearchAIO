using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Search;

public class SearchEndpoint : EndpointBaseAsync.WithRequest<DoSearchRequest>.WithActionResult<DoSearchResponse>
{
    private readonly IDoSearchService _doSearchService;
    private readonly ILogger _logger;

    public SearchEndpoint(IDoSearchService searchService, ILoggerFactory loggerFactory)
    {
        _doSearchService = searchService;
        _logger = loggerFactory.CreateLogger<SearchEndpoint>();
    }

    [HttpPost("/api/search/doSearch")]
    [SwaggerOperation(
        Summary = "search documents",
        Description = "search documents for the given word or phrases, filtered by given aspects",
        OperationId = "B717827F-680A-4068-842F-B4E9512C6369",
        Tags = new[] {"Search"}
    )]
    [ProducesResponseType(typeof(DoSearchResponse), 200)]
    public override async Task<ActionResult<DoSearchResponse>> HandleAsync([FromBody] DoSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Search request received with term {SearchTerm}", request.SearchPhrase);
        return await _doSearchService.DoSearch(request);
    }
}