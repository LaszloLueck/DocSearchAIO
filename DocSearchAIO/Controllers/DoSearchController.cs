using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("api/search")]
    public partial class DoSearchController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DoSearchService _doSearchService;
        private readonly SearchSuggestService _searchSuggestService;
        private readonly DocumentDetailService _documentDetailService;

        public DoSearchController(IElasticClient elasticClient, ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<DoSearchController>();
            _doSearchService = new DoSearchService(elasticClient, loggerFactory, viewToStringRenderer);
            _searchSuggestService = new SearchSuggestService(elasticClient, loggerFactory);
            _documentDetailService = new DocumentDetailService(elasticClient, loggerFactory, viewToStringRenderer);
        }

        [Route("doSearch")]
        [HttpPost]
        public async Task<DoSearchResponse> Index(DoSearchRequest doSearchRequest)
        {
            _logger.LogInformation("Search request received.");
            return await _doSearchService.DoSearch(doSearchRequest);
        }
        
        [Route("doSuggest")]
        [HttpPost]
        public async Task<SuggestResult> Post(SuggestRequest request)
        {
            _logger.LogInformation($"Hit SuggestResult with Phrase {request.SearchPhrase}");
            return await _searchSuggestService.GetSuggestions(request.SearchPhrase);
        }
        
        [Route("documentDetail")]
        [HttpPost]
        public async Task<DocumentDetailResponse> GetDocumentDetail(DocumentDetailRequest request)
        {
            _logger.LogInformation($"get documentdetails for {request.Id}");
            return await _documentDetailService.GetDocumentDetail(request);
        }
        
    }
}