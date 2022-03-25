using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(
        [property: JsonPropertyName("searchResults")] IEnumerable<DoSearchResultContainer> SearchResults, 
        [property: JsonPropertyName("searchResult")] DoSearchResult SearchResult, 
        [property: JsonPropertyName("statistics")] SearchStatisticsModel Statistics);
}