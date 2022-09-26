using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace DocSearchAIO.Endpoints.DownloadFile;

public sealed class FileDownloadRequest
{
    [FromQuery(Name = "path"), Required] public string? Path { get; set; }

    [FromQuery(Name = "documentType"), Required]
    public string? DocumentType { get; set; }
}