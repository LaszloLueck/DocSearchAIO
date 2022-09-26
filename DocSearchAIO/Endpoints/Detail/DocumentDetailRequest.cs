using System.Text.Json.Serialization;

namespace DocSearchAIO.Endpoints.Detail;

public sealed record DocumentDetailRequest([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("indexName")] string IndexName);