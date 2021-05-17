using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResultContainer
    {
        public string RelativeUrl { get; set; }
        public IEnumerable<ContentTypeAndValues> SearchBody { get; set; }
        public double Relevance { get; set; }
        public string ProgramIcon { get; set; }
        public string Id { get; set; }
        public string AbsoluteUrl { get; set; }
        public string DocumentType { get; set; }
    }
}