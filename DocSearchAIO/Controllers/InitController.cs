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
    [Route("api/base")]
    public class InitController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly InitService _initService;
        private readonly FileDownloadService _fileDownloadService;

        public InitController(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<InitController>();
            _initService = new InitService(elasticSearchService, configuration);
            _fileDownloadService = new FileDownloadService();
        }
        

        [Route("init")]
        [HttpPost]
        public async Task<InitResponseObject> Init(InitRequest initRequest)
        {
            _logger.LogInformation("calling init");
            return await _initService.Init(initRequest);
        }
        
        [Route("download")]
        [HttpGet]
        public async Task FileFromAbsolutePath(string path, string documentType)
        {
            var returnValue = _fileDownloadService.DownloadFileStream(path, documentType);
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{returnValue.ReturnFileName}\"");
            Response.ContentType = returnValue.ContentType;
            await using var fs = returnValue.DownloadFileStream;
            await fs.CopyToAsync(Response.Body);
        }
    }
}