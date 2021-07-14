namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResponse(string Pagination, string SearchResults, string Title, string SearchPhrase,
        string Statistics);

}