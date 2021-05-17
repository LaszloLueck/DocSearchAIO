using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class ContentTypeAndValues
    {
        public string ContentType { get; set; }
        public IEnumerable<string> ContentValues { get; set; }
    }
}