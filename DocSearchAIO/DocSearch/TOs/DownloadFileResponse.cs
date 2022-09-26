using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record DownloadFileResponse(FileStream DownloadFileStream, string ReturnFileName, string ContentType);