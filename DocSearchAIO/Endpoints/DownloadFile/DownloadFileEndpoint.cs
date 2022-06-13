using Ardalis.ApiEndpoints;
using DocSearchAIO.DocSearch.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace DocSearchAIO.Endpoints.DownloadFile;

public class DownloadFileEndpoint : EndpointBaseAsync.WithRequest<FileDownloadRequest>.WithActionResult
{
    private readonly IFileDownloadService _fileDownloadService;

    public DownloadFileEndpoint(IFileDownloadService fileDownloadService)
    {
        _fileDownloadService = fileDownloadService;
    }

    [HttpGet("/api/base/download")]
    [SwaggerOperation(
        Summary = "download a file from system",
        Description = "download a file from filesystem path",
        OperationId = "7E5029A8-6763-4428-90C7-CD1EFFB8E6F9",
        Tags = new[] {"FileDownload"}
    )]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public override async Task<ActionResult> HandleAsync([FromQuery] FileDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        if ((request.Path is null || request.Path.Length == 0) ||
            (request.DocumentType is null || request.DocumentType.Length == 0))
            return Problem(detail: "Either path and documentType should not be null", statusCode: 500,
                title: "Parameter is null");

        if (!System.IO.File.Exists(request.Path))
            return Problem(detail: $"File <{request.Path}> does not exists", statusCode: 404, title: "File not exists");

        var returnValue = _fileDownloadService.DownloadFileStream(request.Path!, request.DocumentType!);
        var ms = new MemoryStream();
        await returnValue.DownloadFileStream.CopyToAsync(ms, cancellationToken);
        returnValue.DownloadFileStream.Close();
        await returnValue.DownloadFileStream.DisposeAsync();
        ms.Position = 0;
        return new FileStreamResult(ms, returnValue.ContentType)
        {
            FileDownloadName = returnValue.ReturnFileName
        };
    }
}