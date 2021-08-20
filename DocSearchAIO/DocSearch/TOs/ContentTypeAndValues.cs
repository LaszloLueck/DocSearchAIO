using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{

    public record ContentTypeAndValues(IEnumerable<ContentDetail> ContentValues);
}