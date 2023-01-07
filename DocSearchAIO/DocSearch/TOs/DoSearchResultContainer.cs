using System.Text.Json.Serialization;
using DocSearchAIO.Classes;
using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record DoSearchResultContainer(
    [property: JsonPropertyName("relativeUrl")] string RelativeUrl,
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("processTime")] DateTime ProcessTime,
    [property: JsonPropertyName("absoluteUrl")] string? AbsoluteUrl,
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("indexName")] string IndexName
)
{
    [property: JsonPropertyName("contents")] public IEnumerable<ContentDetail> Contents { get; set; } = System.Array.Empty<ContentDetail>();
    [property: JsonPropertyName("comments")] public IEnumerable<CommentDetail> Comments { get; set; } = System.Array.Empty<CommentDetail>();
    [property: JsonPropertyName("relevance")] public double Relevance { get; set; }
    [property: JsonPropertyName("programIcon")] public string ProgramIcon { get; set; } = null!;
}