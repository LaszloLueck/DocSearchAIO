using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
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
    private readonly IConfigurationUpdater _configurationUpdater;

    public OptionDialogService(IElasticSearchService elasticSearchService, IConfigurationUpdater configurationUpdater)
    {
        _elasticSearchService = elasticSearchService;
        _configurationUpdater = configurationUpdater;
    }

    public async Task<OptionDialogResponse> OptionDialog(
        OptionDialogRequest optionDialogRequest)
    {
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var indexResponse = await _elasticSearchService.IndicesWithPatternAsync($"{cfg.IndexName}-*");
        var knownIndices = indexResponse.IndexNames;

        OptionDialogResponse response = optionDialogRequest;
        response.WordIndexExists = StaticHelpers.IndexKeyExpression<WordElasticDocument>(cfg, knownIndices);
        response.ExcelIndexExists = StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(cfg, knownIndices);
        response.PowerpointIndexExists = StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(cfg, knownIndices);
        response.PdfIndexExists = StaticHelpers.IndexKeyExpression<PdfElasticDocument>(cfg, knownIndices);
        response.MsgIndexExists = StaticHelpers.IndexKeyExpression<MsgElasticDocument>(cfg, knownIndices);
        response.EmlIndexExists = StaticHelpers.IndexKeyExpression<EmlElasticDocument>(cfg, knownIndices);
        return response;
    }
}