using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.DocSearch.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InitController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly InitService _initService;

        public InitController(ILoggerFactory loggerFactory, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<InitController>();
            _initService = new InitService(loggerFactory, elasticClient);
        }
        

        [HttpPost]
        public async Task<InitResponseObject> Init(InitRequestObject initRequestObject)
        {
            _logger.LogInformation("calling init");
            return await _initService.Init(initRequestObject);
        }
    }
}