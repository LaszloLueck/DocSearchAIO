using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
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

        public async Task<InitResponseObject> Init(InitRequestObject initRequestObject)
        {
            var indicesResponse = await _elasticSearchService.GetIndicesWithPatternAsync("officedocuments-*");
            var indices = indicesResponse.Indices.Keys.Select(p => p.Name);

            var returnValue = new InitResponseObject();
            var enumerable = indices as string[] ?? indices.ToArray();
            returnValue.FilterWord = enumerable.Contains("officedocuments-word") && initRequestObject.FilterWord;
            returnValue.FilterExcel = enumerable.Contains("officedocuments-excel") && initRequestObject.FilterExcel;
            returnValue.FilterPowerpoint =
                enumerable.Contains("officedocuments-powerpoint") && initRequestObject.FilterPowerpoint;
            returnValue.FilterPdf = enumerable.Contains("officedocuments-pdf") && initRequestObject.FilterPdf;
            returnValue.WordFilterActive = enumerable.Contains("officedocuments-word");
            returnValue.ExcelFilterActive = enumerable.Contains("officedocuments-excel");
            returnValue.PowerpointFilterActive = enumerable.Contains("officedocuments-powerpoint");
            returnValue.PdfFilterActive = enumerable.Contains("officedocuments-pdf");
            
            returnValue.ItemsPerPage = initRequestObject.ItemsPerPage ?? 20;

            return returnValue;
        }
    }
}