namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResponse
        {
            public string Pagination { get; set; }
            public string SearchResults { get; set; }
            public string Title { get; set; }
            public string SearchPhrase { get; set; }
            
            public string Statistics { get; set; }
        }
    }