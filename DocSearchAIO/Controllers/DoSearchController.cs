using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;

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

        // [Route("doSearch")]
        // [HttpPost]
        // public async Task<IActionResult> Index(DoSearchRequest doSearchRequest)
        // {
        //     var httpContext = this.HttpContext;
        //     var response = httpContext.Response;
        //     _logger.LogInformation("Search request received");
        //     var returnValue = await _doSearchService.DoSearch(doSearchRequest);
        //     httpContext.Response.ContentType = "text/json";
        //     var jsonString = JsonSerializer.Serialize(returnValue);
        //     var b = Encoding.UTF8.GetBytes(jsonString);
        //     response.Headers.Add("Content-Length", b.Length.ToString());
        //     try
        //     {
        //         await response.Body.WriteAsync(b);
        //         await response.Body.FlushAsync();
        //         response.Body.Close();
        //         return new EmptyResult();
        //     }
        //     catch (Exception exception)
        //     {
        //         _logger.LogError(exception, "An error occured");
        //         return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        //     }
        //
        // }

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