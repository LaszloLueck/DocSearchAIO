using System.IO;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DownloadFileResponse(Stream DownloadFileStream, string ReturnFileName, string ContentType);
}