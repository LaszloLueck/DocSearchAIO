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
            return new(
                StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(_cfg, enumerable, initRequest.FilterExcel),
                StaticHelpers.IndexKeyExpression<WordElasticDocument>(_cfg, enumerable, initRequest.FilterWord),
                StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(_cfg, enumerable, initRequest.FilterPowerpoint),
                StaticHelpers.IndexKeyExpression<PdfElasticDocument>(_cfg, enumerable, initRequest.FilterPdf),
                StaticHelpers.IndexKeyExpression<MsgElasticDocument>(_cfg, enumerable, initRequest.FilterMsg),
                StaticHelpers.IndexKeyExpression<EmlElasticDocument>(_cfg, enumerable, initRequest.FilterEml),
                initRequest.ItemsPerPage,
                StaticHelpers.IndexKeyExpression<WordElasticDocument>(_cfg, enumerable),
                StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(_cfg, enumerable),
                StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(_cfg, enumerable),
                StaticHelpers.IndexKeyExpression<PdfElasticDocument>(_cfg, enumerable),
                StaticHelpers.IndexKeyExpression<MsgElasticDocument>(_cfg, enumerable),
                StaticHelpers.IndexKeyExpression<EmlElasticDocument>(_cfg, enumerable)
            );
        }
    }
}