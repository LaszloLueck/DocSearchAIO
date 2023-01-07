using DocSearchAIO.Classes;

namespace DocSearchAIO.Endpoints.Detail;

public sealed record DocumentDetailModel(string Creator, DateTime Created, string LastModifiedBy, DateTime LastModified,
    string Title, string Subject, string Version, string Revision, DateTime LastPrinted, string FileName,
    DateTime LastProcessTime)
{
    public string Id { get; set; } = null!;

}