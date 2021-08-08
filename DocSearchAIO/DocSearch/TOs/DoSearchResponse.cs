namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(string SearchResults, string Title, string SearchPhrase, long DocumentCount, int PageSize, int CurrentPage,
        string Statistics);
}