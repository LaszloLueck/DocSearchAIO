namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResponse
    {
        public string Pagination { get; set; } = string.Empty;
        public string SearchResults { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SearchPhrase { get; set; } = string.Empty;
        public string Statistics { get; set; } = string.Empty;
    }
}