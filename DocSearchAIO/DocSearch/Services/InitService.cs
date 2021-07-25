using System;
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
    public class InitService
    {
        private readonly ILogger _logger;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ConfigurationObject _cfg;

        public InitService(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<InitService>();
            _elasticSearchService = elasticSearchService;
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
        }

        public async Task<InitResponseObject> Init(InitRequest initRequest)
        {
            var indicesResponse = await _elasticSearchService.IndicesWithPatternAsync($"{_cfg.IndexName}-*");
            var indices = indicesResponse.Indices.Keys.Select(p => p.Name);
            var enumerable = indices.ResolveNullable(Array.Empty<string>(), (v, _) => v.ToArray());
            return new InitResponseObject(
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(ExcelElasticDocument)) &&
                initRequest.FilterExcel,
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(WordElasticDocument)) &&
                initRequest.FilterWord,
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable,
                    nameof(PowerpointElasticDocument)) && initRequest.FilterPowerpoint,
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(PdfElasticDocument)) &&
                initRequest.FilterPdf,
                initRequest.ItemsPerPage,
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(WordElasticDocument)),
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(ExcelElasticDocument)),
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable,
                    nameof(PowerpointElasticDocument)),
                StaticHelpers.GetIndexKeyExpressionFromConfiguration(_cfg, enumerable, nameof(PdfElasticDocument))
            );
        }
    }
}