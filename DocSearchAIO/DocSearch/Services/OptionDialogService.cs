using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class OptionDialogService
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
            var knownIndices = indexResponse.Indices.Keys.Select(key => key.Name).ToArray();

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
}