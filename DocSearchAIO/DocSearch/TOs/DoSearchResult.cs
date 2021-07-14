namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResult(int CurrentPage, int CurrentPageSize, long DocCount, string SearchPhrase);
}