using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class DoSearchController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DoSearchService _doSearchService;
        private readonly SearchSuggestService _searchSuggestService;
        private readonly DocumentDetailService _documentDetailService;

        public DoSearchController(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<DoSearchController>();
            _doSearchService =
                new DoSearchService(elasticSearchService, loggerFactory, configuration);
            _searchSuggestService = new SearchSuggestService(elasticSearchService, loggerFactory, configuration);
            _documentDetailService =
                new DocumentDetailService(elasticSearchService, loggerFactory);
        }

        [Route("doSearch")]
        [HttpPost]
        public async Task<DoSearchResponse> Index(DoSearchRequest doSearchRequest)
        {
            _logger.LogInformation("Search request received");
            return await _doSearchService.DoSearch(doSearchRequest);
        }

        [Route("doSuggest")]
        [HttpPost]
        public async Task<SuggestResult> Post(SuggestRequest request)
        {
            _logger.LogInformation("hit suggestResult with phrase {SearchPhrase}", request.SearchPhrase);
            return await _searchSuggestService.Suggestions(request.SearchPhrase);
        }

        [Route("documentDetailData")]
        [HttpPost]
        public async Task<DocumentDetailModel> DocumentDetailData(DocumentDetailRequest request)
        {
            _logger.LogInformation("get document details for {RequestId}", request.Id);
            return await _documentDetailService.DocumentDetail(request);
        }
    }
}