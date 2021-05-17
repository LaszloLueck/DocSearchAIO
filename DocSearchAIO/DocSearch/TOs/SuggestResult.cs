using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SuggestResult
    {
        public long DocCount { get; set; }
        public string SearchPhrase { get; set; }
        public long SearchTime { get; set; }
        
        public IEnumerable<SuggestEntry> suggests { get; set; }
    }
}