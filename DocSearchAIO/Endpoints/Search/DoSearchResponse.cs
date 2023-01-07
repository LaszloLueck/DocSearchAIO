using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.TOs;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Search;

[Record]
public sealed record DoSearchResponse(
    [property: JsonPropertyName("searchResults")] IEnumerable<DoSearchResultContainer> SearchResults,
    [property: JsonPropertyName("searchResult")] DoSearchResult SearchResult,
    [property: JsonPropertyName("statistics")] SearchStatisticsModel Statistics);