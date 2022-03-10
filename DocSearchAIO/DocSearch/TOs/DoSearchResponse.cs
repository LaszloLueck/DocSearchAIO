namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(IEnumerable<DoSearchResultContainer> SearchResults, DoSearchResult SearchResult, SearchStatisticsModel Statistics);
}