namespace DocSearchAIO.DocSearch.TOs;

public record DownloadFileResponse(FileStream DownloadFileStream, string ReturnFileName, string ContentType);