using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SuggestResult
    {
        public string SearchPhrase { get; set; } = string.Empty;
        public IEnumerable<SuggestEntry> Suggests { get; set; } = Array.Empty<SuggestEntry>();
    }
}