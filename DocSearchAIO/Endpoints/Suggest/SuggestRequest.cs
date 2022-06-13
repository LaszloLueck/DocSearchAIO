using System.Text.Json.Serialization;

namespace DocSearchAIO.Endpoints.Suggest;

public record SuggestRequest([property: JsonPropertyName("searchPhrase")] string SearchPhrase);