using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record ContentDetail(
    [property: JsonPropertyName("contentText")] string ContentText
);