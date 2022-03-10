namespace DocSearchAIO.DocSearch.TOs
{
    public record SuggestResult(string SearchPhrase, IEnumerable<SuggestEntry> Suggests);
}