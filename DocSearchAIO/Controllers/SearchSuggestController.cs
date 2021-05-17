using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchSuggestController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly SearchSuggestService _searchSuggestService;
        
        public SearchSuggestController(IElasticClient elasticClient, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SearchSuggestController>();
            _searchSuggestService = new SearchSuggestService(elasticClient, loggerFactory);
        }
        
        
        [HttpPost]
        public async Task<SuggestResult> Post(SuggestRequest request)
        {
            _logger.LogInformation($"Hit SuggestResult with Phrase {request.SearchPhrase}");
            return await _searchSuggestService.GetSuggestions(request.SearchPhrase);
        }
    }
}