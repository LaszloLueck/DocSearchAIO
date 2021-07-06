using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class ContentTypeAndValues
    {
        public string ContentType { get; set; } = string.Empty;
        public IEnumerable<string> ContentValues { get; set; } = Array.Empty<string>();

    }
}