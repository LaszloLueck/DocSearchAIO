using System.IO;
using System.Web;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class FileDownloadService
    {
        private readonly ILogger _logger;
        
        public FileDownloadService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FileDownloadService>();
        }

        public DownloadFileResponse GetDownloadFileStream(string path, string documentType)
        {
            var contentType = documentType switch
            {
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "pdf" => "application/pdf",
                _ => documentType
            };

            var returnFileName = HttpUtility.UrlEncode(Path.GetFileName(path));
            
            var downloadFileResponse = new DownloadFileResponse
            {
                ReturnFileName = returnFileName,
                DownloadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096),
                ContentType = contentType
            };
            return downloadFileResponse;
        }
        
    }
}