using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResult(
        [property: JsonPropertyName("currentPage")] int CurrentPage, 
        [property: JsonPropertyName("currentPageSize")] int CurrentPageSize, 
        [property: JsonPropertyName("docCount")] long DocCount, 
        [property: JsonPropertyName("searchPhrase")] string SearchPhrase
        );
}