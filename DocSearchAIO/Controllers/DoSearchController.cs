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
    [Route("[controller]")]
    public partial class DoSearchController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DoSearchService _doSearchService;

        public DoSearchController(IElasticClient elasticClient, ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<DoSearchController>();
            _doSearchService = new DoSearchService(elasticClient, loggerFactory, viewToStringRenderer);
        }

        [HttpPost]
        public async Task<DoSearchResponse> Index(DoSearchRequest doSearchRequest)
        {
            _logger.LogInformation("Search request received.");
            return await _doSearchService.DoSearch(doSearchRequest);
        }
    }
}