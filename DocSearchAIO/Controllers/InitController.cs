using System.IO;
using System.Threading.Tasks;
using System.Web;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("api/base")]
    public class InitController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly InitService _initService;
        private readonly FileDownloadService _fileDownloadService;

        public InitController(ILoggerFactory loggerFactory, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<InitController>();
            _initService = new InitService(loggerFactory, elasticSearchService);
            _fileDownloadService = new FileDownloadService(loggerFactory);
        }
        

        [Route("init")]
        [HttpPost]
        public async Task<InitResponseObject> Init(InitRequestObject initRequestObject)
        {
            _logger.LogInformation("calling init");
            return await _initService.Init(initRequestObject);
        }
        
        [Route("download")]
        [HttpGet]
        public async Task GetFileFromAbsolutePath(string path, string documentType)
        {
            var returnValue = _fileDownloadService.GetDownloadFileStream(path, documentType);
            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{returnValue.ReturnFileName}\"");
            Response.ContentType = returnValue.ContentType;
            await using var fs = returnValue.DownloadFileStream;
            await fs.CopyToAsync(Response.Body);
        }
    }
}