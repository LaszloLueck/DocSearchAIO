using System;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class InitService
    {
        private readonly ILogger _logger;
        private readonly IElasticSearchService _elasticSearchService;

        public InitService(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<InitService>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task<InitResponseObject> Init(InitRequest initRequest)
        {
            var indicesResponse = await _elasticSearchService.IndicesWithPatternAsync("officedocuments-*");
            var indices = indicesResponse.Indices.Keys.Select(p => p.Name);
            var enumerable = indices.ResolveNullable(Array.Empty<string>(), (v, _) => v.ToArray());
            return new InitResponseObject(
                enumerable.Contains("officedocuments-excel") && initRequest.FilterExcel,
                enumerable.Contains("officedocuments-word") && initRequest.FilterWord,
                enumerable.Contains("officedocuments-powerpoint") && initRequest.FilterPowerpoint,
                enumerable.Contains("officedocuments-pdf") && initRequest.FilterPdf,
                initRequest.ItemsPerPage,
                enumerable.Contains("officedocuments-word"),
                enumerable.Contains("officedocuments-excel"),
                enumerable.Contains("officedocuments-powerpoint"),
                enumerable.Contains("officedocuments-pdf")
            );
        }
    }
}