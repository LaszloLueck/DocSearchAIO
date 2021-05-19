using System.IO;

namespace DocSearchAIO.DocSearch.TOs
{
    public class DownloadFileResponse
    {
        public Stream DownloadFileStream { get; set; }
        public string ReturnFileName { get; set; }
        public string ContentType { get; set; }
    }
}