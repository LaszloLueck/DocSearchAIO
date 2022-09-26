using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record DoSearchResult(
    [property: JsonPropertyName("currentPage")] int CurrentPage, 
    [property: JsonPropertyName("currentPageSize")] int CurrentPageSize, 
    [property: JsonPropertyName("docCount")] long DocCount, 
    [property: JsonPropertyName("searchPhrase")] string SearchPhrase
);