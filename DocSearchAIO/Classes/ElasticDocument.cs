using Nest;

namespace DocSearchAIO.Classes;

public class ElasticDocument
{

    [Text(Name = "id")] public string Id { get; set; } = null!;

    [Text(Name = "contentHash")] public string ContentHash { get; set; } = null!;

    [Text(Name = "originalFilePath")] public string OriginalFilePath { get; set; } = null!;

    [Text(Name = "uriFilePath")] public string UriFilePath { get; set; } = null!;

    [Text(Name = "content", TermVector = TermVectorOption.WithPositionsOffsetsPayloads)]
    public string Content { get; set; } = null!;

    [Completion(Name = "completionContent")]
    public CompletionField CompletionContent { get; set; } = new();

    [Text(Name = "createdBy")] public string Creator { get; set; } = null!;

    [Text(Name = "keywords")] public string[] Keywords { get; set; } = System.Array.Empty<string>();

    [Text(Name = "subject")] public string Subject { get; set; } = null!;

    [Date(Name = "processTime")] public DateTime ProcessTime { get; set; }

    [Text(Name = "title")] public string Title { get; set; } = null!;

    [Text(Name = "contentType")] public string ContentType { get; set; } = null!;

    [Object(Name = "comments")]
    public IEnumerable<OfficeDocumentComment> Comments { get; set; } = System.Array.Empty<OfficeDocumentComment>();
}