using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Controllers;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.DocSearch.Services
{
    public class OptionDialogService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly ElasticSearchService _elasticSearchService;

        public OptionDialogService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<OptionDialogService>();
            _viewToStringRenderer = viewToStringRenderer;
            _elasticSearchService = new ElasticSearchService(loggerFactory, elasticClient);
        }

        public async Task<OptionDialogController.OptionDialogResponse> GetOptionDialog(
            OptionDialogRequest optionDialogRequest)
        {
            var indexResponse = await _elasticSearchService.GetIndicesWithPatternAsync("officedocuments-*");
            var knownIndices = indexResponse.Indices.Keys.Select(key => key.Name);

            optionDialogRequest.WordIndexExists = knownIndices.Contains("officedocuments-word");
            optionDialogRequest.ExcelIndexExists = knownIndices.Contains("officedocuments-excel");
            optionDialogRequest.PowerpointIndexExists = knownIndices.Contains("officedocuments-powerpoint");
            optionDialogRequest.PdfIndexExists = knownIndices.Contains("officedocuments-pdf");

            var html = await _viewToStringRenderer.Render("ResultPageConfigurationModalPartial", optionDialogRequest);
            return new OptionDialogController.OptionDialogResponse()
                {State = "OK", Content = html, ElementName = "#optionModal"};
        }
    }
}