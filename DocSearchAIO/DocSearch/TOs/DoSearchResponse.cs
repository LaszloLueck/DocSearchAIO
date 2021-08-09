using System.Collections;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(IEnumerable<DoSearchResultContainer> SearchResults, DoSearchResult SearchResult, SearchStatisticsModel Statistics);
}