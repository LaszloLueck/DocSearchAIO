using System;
using System.Collections.Generic;
using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResultContainer(string RelativeUrl, string Id, string AbsoluteUrl, string DocumentType)
    {
        public IEnumerable<ContentTypeAndValues> SearchBody { get; set; } = Array.Empty<ContentTypeAndValues>();
        public double Relevance { get; set; }
        public string ProgramIcon { get; set; } = string.Empty;
        
        public static implicit operator DoSearchResultContainer(ElasticDocument document) =>
            new(document.UriFilePath, document.Id, document.OriginalFilePath, document.ContentType);
    }
}