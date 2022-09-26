using System.Text.Json.Serialization;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace DocSearchAIO.Endpoints.Suggest;

[Record]
public sealed record SuggestRequest(
    [FromBody] [property: JsonPropertyName("searchPhrase")]
    string SearchPhrase,
    [FromBody] [property: JsonPropertyName("suggestWord")]
    bool SuggestWord,
    [FromBody] [property: JsonPropertyName("suggestExcel")]
    bool SuggestExcel,
    [FromBody] [property: JsonPropertyName("suggestPowerpoint")]
    bool SuggestPowerpoint,
    [FromBody] [property: JsonPropertyName("suggestPdf")]
    bool SuggestPdf,
    [FromBody] [property: JsonPropertyName("suggestEml")]
    bool SuggestEml,
    [FromBody] [property: JsonPropertyName("SuggestMsg")]
    bool SuggestMsg
);