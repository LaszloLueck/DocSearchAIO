using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.TOs;

namespace DocSearchAIO.Endpoints.Search;

public record DoSearchResponse(
    [property: JsonPropertyName("searchResults")] IEnumerable<DoSearchResultContainer> SearchResults, 
    [property: JsonPropertyName("searchResult")] DoSearchResult SearchResult, 
    [property: JsonPropertyName("statistics")] SearchStatisticsModel Statistics);