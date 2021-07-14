using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public record SuggestResult(string SearchPhrase, IEnumerable<SuggestEntry> Suggests);
}