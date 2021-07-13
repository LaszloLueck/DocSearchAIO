using System;
using System.Collections.Generic;
using DocSearchAIO.Classes;
using DocSearchAIO.Utilities;
using Nest;

namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchResultContainer
    {
        public string RelativeUrl { get; set; } = string.Empty;
        public IEnumerable<ContentTypeAndValues> SearchBody { get; set; } = Array.Empty<ContentTypeAndValues>();
        public double Relevance { get; set; }
        public string ProgramIcon { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string AbsoluteUrl { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;

        public static implicit operator DoSearchResultContainer(ElasticDocument document) => new()
        {
            AbsoluteUrl = document.OriginalFilePath,
            DocumentType = document.ContentType,
            Id = document.Id,
            RelativeUrl = document.UriFilePath
        };
    }
}