using System.Text.Json.Serialization;
using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs;

public record DoSearchResultContainer(
    [property: JsonPropertyName("relativeUrl")] string RelativeUrl, 
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("processTime")] DateTime ProcessTime,
    [property: JsonPropertyName("absoluteUrl")] string AbsoluteUrl, 
    [property: JsonPropertyName("documentType")] string DocumentType
)
{
    [property: JsonPropertyName("contents")] public IEnumerable<ContentDetail> Contents { get; set; } = Array.Empty<ContentDetail>();
    [property: JsonPropertyName("comments")] public IEnumerable<CommentDetail> Comments { get; set; } = Array.Empty<CommentDetail>();
    [property: JsonPropertyName("relevance")] public double Relevance { get; set; }
    [property: JsonPropertyName("programIcon")] public string ProgramIcon { get; set; } = string.Empty;

    public static implicit operator DoSearchResultContainer(ElasticDocument document) =>
        new(document.UriFilePath, document.Id,document.ProcessTime, document.OriginalFilePath, document.ContentType);
}