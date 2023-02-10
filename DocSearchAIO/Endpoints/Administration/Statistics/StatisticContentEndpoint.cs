using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.Administration.Statistics;

public class StatisticContentEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<IndexStatistic>
{
    private readonly IAdministrationService _administrationService;
    private readonly ILogger _logger;

    public StatisticContentEndpoint(IAdministrationService administrationService, ILoggerFactory loggerFactory)
    {
        _administrationService = administrationService;
        _logger = LoggingFactoryBuilder.Build<StatisticContentEndpoint>();
    }

    [HttpGet("/api/administration/getStatisticContentData")]
    [SwaggerOperation(
        Summary = "receive statistic content information",
        Description = "receive statistic content information",
        OperationId = "26AA1AFC-5D8A-4316-BF76-B97249B50675",
        Tags = new[] { "Administration" }
    )]
    [ProducesResponseType(typeof(IndexStatistic), 200)]
    public override async Task<ActionResult<IndexStatistic>> HandleAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("method getStatisticsContentData called");
        return await _administrationService.StatisticsContent();
    }
}