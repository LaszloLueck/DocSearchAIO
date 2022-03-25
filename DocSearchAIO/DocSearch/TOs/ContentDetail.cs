using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public record ContentDetail(
        [property: JsonPropertyName("contentText")] string ContentText
        );
}