namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResponse
        {
            public string Pagination { get; set; }
            public string SearchResults { get; set; }
            public long DocCount { get; set; }
            public long SearchTime { get; set; }
            public string Title { get; set; }
            public string SearchPhrase { get; set; }
        }
    }