namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResult
    {
        public int CurrentPage { get; set; }
        public int CurrentPageSize { get; set; }
        public long DocCount { get; set; }
        public string SearchPhrase { get; set; } = string.Empty;
    }
}