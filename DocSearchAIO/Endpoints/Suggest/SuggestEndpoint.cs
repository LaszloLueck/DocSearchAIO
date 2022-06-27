using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Suggest;

public class SuggestEndpoint : EndpointBaseAsync.WithRequest<SuggestRequest>.WithActionResult<SuggestResult>
{
    private readonly ISearchSuggestService _searchSuggestService;
    private readonly ILogger _logger;

    public SuggestEndpoint(ISearchSuggestService searchSuggestService, ILoggerFactory loggerFactory)
    {
        _searchSuggestService = searchSuggestService;
        _logger = loggerFactory.CreateLogger<SuggestEndpoint>();
    }

    [HttpPost("/api/search/doSuggest")]
    [SwaggerOperation(
        Summary = "suggest words",
        Description = "delivers a list of suggested words as typed in the search field",
        OperationId = "E96F58A0-E657-45B1-A996-666F89F6F559",
        Tags = new[] {"Suggest"}
    )]
    [ProducesResponseType(typeof(SuggestResult), 200)]
    public override async Task<ActionResult<SuggestResult>> HandleAsync(SuggestRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("hit suggestResult with phrase {SearchPhrase}", request.SearchPhrase);
        return await _searchSuggestService.Suggestions(request);
    }
}