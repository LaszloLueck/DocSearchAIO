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

            var returnValue = new InitResponseObject();
            var enumerable = indices.ResolveNullable(Array.Empty<string>(), (v, _) => v.ToArray());
            returnValue.FilterWord = enumerable.Contains("officedocuments-word") && initRequest.FilterWord;
            returnValue.FilterExcel = enumerable.Contains("officedocuments-excel") && initRequest.FilterExcel;
            returnValue.FilterPowerpoint =
                enumerable.Contains("officedocuments-powerpoint") && initRequest.FilterPowerpoint;
            returnValue.FilterPdf = enumerable.Contains("officedocuments-pdf") && initRequest.FilterPdf;
            returnValue.WordFilterActive = enumerable.Contains("officedocuments-word");
            returnValue.ExcelFilterActive = enumerable.Contains("officedocuments-excel");
            returnValue.PowerpointFilterActive = enumerable.Contains("officedocuments-powerpoint");
            returnValue.PdfFilterActive = enumerable.Contains("officedocuments-pdf");
            
            returnValue.ItemsPerPage = initRequest.ItemsPerPage;

            return returnValue;
        }
    }
}