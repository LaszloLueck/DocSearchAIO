using System.IO;
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

        public Stream GetDownloadFileStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 4096);
        }
        
    }
}