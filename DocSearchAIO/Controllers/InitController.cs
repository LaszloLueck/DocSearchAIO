using System.IO;
using System.Threading.Tasks;
using System.Web;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
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

        public InitController(ILoggerFactory loggerFactory, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<InitController>();
            _initService = new InitService(loggerFactory, elasticClient);
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
            Response.ContentType = documentType switch
            {
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "pdf" => "application/pdf",
                _ => Response.ContentType
            };

            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{HttpUtility.UrlEncode(Path.GetFileName(path))}\"");
            await using var fs = _fileDownloadService.GetDownloadFileStream(path);
            await fs.CopyToAsync(Response.Body);
        }
    }
}