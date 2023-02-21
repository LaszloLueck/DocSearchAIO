using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Endpoints.Init;
using DocSearchAIO.Services;
using MethodTimer;

namespace DocSearchAIO.DocSearch.Services;

public interface IInitService
{
    public Task<InitResponseObject> Init(InitRequest initRequest);
}

public class InitService : IInitService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly IConfigurationUpdater _configurationUpdater;

    public InitService(IElasticSearchService elasticSearchService, IConfigurationUpdater configurationUpdater)
    {
        _elasticSearchService = elasticSearchService;
        _configurationUpdater = configurationUpdater;
    }

    [Time]
    public async Task<InitResponseObject> Init(InitRequest initRequest)
    {
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var indicesResponse = await _elasticSearchService.IndicesWithPatternAsync($"{cfg.IndexName}-*");
        var indexNames = indicesResponse.IndexNames;
        return new InitResponseObject(cfg, indexNames, initRequest);
    }
}