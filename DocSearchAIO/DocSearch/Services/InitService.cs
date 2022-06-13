using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Endpoints.Init;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Nest;

namespace DocSearchAIO.DocSearch.Services;

public interface IInitService
{
    public Task<InitResponseObject> Init(InitRequest initRequest);
}

public class InitService : IInitService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly ConfigurationObject _cfg;

    public InitService(IElasticSearchService elasticSearchService,
        IConfiguration configuration)
    {
        _elasticSearchService = elasticSearchService;
        _cfg = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(_cfg);
    }

    public async Task<InitResponseObject> Init(InitRequest initRequest)
    {
        var indicesResponse = await _elasticSearchService.IndicesWithPatternAsync($"{_cfg.IndexName}-*");
        var indexNames = indicesResponse
            .Indices
            .OrElseIfNull(new Dictionary<IndexName, IndexState>())
            .Map(kv => kv.Key.Name)
            .ToArray();

        return new InitResponseObject(_cfg, indexNames, initRequest);
    }
}