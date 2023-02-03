using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Endpoints.Administration.Options;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;

namespace DocSearchAIO.DocSearch.Services;

public interface IOptionDialogService
{
    public Task<OptionDialogResponse> OptionDialog(OptionDialogRequest optionDialogRequest);
}

public class OptionDialogService : IOptionDialogService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ConfigurationObject _cfg;

    public OptionDialogService(IElasticSearchService elasticSearchService, IConfiguration configuration)
    {
        _elasticSearchService = elasticSearchService;
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
    }

    public async Task<OptionDialogResponse> OptionDialog(
        OptionDialogRequest optionDialogRequest)
    {
        var indexResponse = await _elasticSearchService.IndicesWithPatternAsync($"{_cfg.IndexName}-*");
        var knownIndices = indexResponse.IndexNames;

        OptionDialogResponse response = optionDialogRequest;
        response.WordIndexExists = StaticHelpers.IndexKeyExpression<WordElasticDocument>(_cfg, knownIndices);
        response.ExcelIndexExists = StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(_cfg, knownIndices);
        response.PowerpointIndexExists = StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(_cfg, knownIndices);
        response.PdfIndexExists = StaticHelpers.IndexKeyExpression<PdfElasticDocument>(_cfg, knownIndices);
        response.MsgIndexExists = StaticHelpers.IndexKeyExpression<MsgElasticDocument>(_cfg, knownIndices);
        response.EmlIndexExists = StaticHelpers.IndexKeyExpression<EmlElasticDocument>(_cfg, knownIndices);
        return response;
    }
}