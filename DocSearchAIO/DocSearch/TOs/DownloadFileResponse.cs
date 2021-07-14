using System.IO;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DownloadFileResponse(Stream DownloadFileStream, string ReturnFileName, string ContentType);

    // public class DownloadFileResponse
    // {
    //     public Stream DownloadFileStream { get; set; } = Stream.Null;
    //     public string ReturnFileName { get; set; } = string.Empty;
    //     public string ContentType { get; set; } = string.Empty;
    // }
}