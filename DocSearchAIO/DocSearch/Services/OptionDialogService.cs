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

        public async Task<OptionDialogRequest> OptionDialog(
            OptionDialogRequest optionDialogRequest)
        {
            var indexResponse = await _elasticSearchService.IndicesWithPatternAsync("officedocuments-*");
            var knownIndices = indexResponse.Indices.Keys.Select(key => key.Name);

            optionDialogRequest.WordIndexExists = knownIndices.Contains("officedocuments-word");
            optionDialogRequest.ExcelIndexExists = knownIndices.Contains("officedocuments-excel");
            optionDialogRequest.PowerpointIndexExists = knownIndices.Contains("officedocuments-powerpoint");
            optionDialogRequest.PdfIndexExists = knownIndices.Contains("officedocuments-pdf");

            return optionDialogRequest;
        }
    }
}