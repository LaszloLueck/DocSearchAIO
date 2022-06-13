using System.Text.Json.Serialization;

namespace DocSearchAIO.Endpoints.Detail;

public record DocumentDetailRequest([property: JsonPropertyName("id")] string Id);