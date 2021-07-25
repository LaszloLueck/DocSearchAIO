using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class OptionDialogService
    {
        private readonly ILogger _logger;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ConfigurationObject _cfg;
        
        public OptionDialogService(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<OptionDialogService>();
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
            response.WordIndexExists = StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, knownIndices, nameof(WordElasticDocument));
            response.ExcelIndexExists = StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, knownIndices, nameof(ExcelElasticDocument));
            response.PdfIndexExists = StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, knownIndices, nameof(PdfElasticDocument));
            response.PowerpointIndexExists = StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, knownIndices, nameof(PowerpointElasticDocument));
            return response;
        }
    }
}