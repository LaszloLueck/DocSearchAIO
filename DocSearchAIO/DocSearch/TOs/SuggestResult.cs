using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SuggestResult
    {
        public string SearchPhrase { get; set; }
        public IEnumerable<SuggestEntry> Suggests { get; set; }
    }
}