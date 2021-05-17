using System.IO;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileDownloadController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly FileDownloadService _fileDownloadService;
        
        public FileDownloadController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileDownloadController>();
            _fileDownloadService = new FileDownloadService(loggerFactory);
        }
        
        
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

            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{Path.GetFileName(path)}\"");
            await using var fs = _fileDownloadService.GetDownloadFileStream(path);
            await fs.CopyToAsync(Response.Body);
        }
    }
}