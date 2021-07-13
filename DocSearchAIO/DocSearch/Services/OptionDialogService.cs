using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class OptionDialogService
    {
        private readonly ILogger _logger;
        private readonly IElasticSearchService _elasticSearchService;

        public OptionDialogService(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<OptionDialogService>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task<OptionDialogResponse> OptionDialog(
            OptionDialogRequest optionDialogRequest)
        {
            var indexResponse = await _elasticSearchService.IndicesWithPatternAsync("officedocuments-*");
            var knownIndices = indexResponse.Indices.Keys.Select(key => key.Name);

            OptionDialogResponse response = optionDialogRequest;
            response.WordIndexExists = knownIndices.Contains("officedocuments-word");
            response.ExcelIndexExists = knownIndices.Contains("officedocuments-excel");
            response.PdfIndexExists = knownIndices.Contains("officedocuments-pdf");
            response.PowerpointIndexExists = knownIndices.Contains("officedocuments-powwerpoint");
            return response;
        }
    }
}