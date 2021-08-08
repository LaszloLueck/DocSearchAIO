namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(string SearchResults, string Title, string SearchPhrase, long documentCount, int pageSize, int currentPage,
        string Statistics);
}