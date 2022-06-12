using System.Web;
using DocSearchAIO.DocSearch.TOs;

namespace DocSearchAIO.DocSearch.Services;

public interface IFileDownloadService
{
    Task<DownloadFileResponse> DownloadFileStream(string path, string documentType);
}

public class FileDownloadService: IFileDownloadService
{
    public async Task<DownloadFileResponse> DownloadFileStream(string path, string documentType)
    {
        static string ContentType(string documentType) => documentType switch
        {
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "pdf" => "application/pdf",
            _ => documentType
        };

        var returnFileName = HttpUtility.UrlEncode(Path.GetFileName(path));
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096);
        return new DownloadFileResponse(fs, returnFileName, ContentType(documentType));
    }
}