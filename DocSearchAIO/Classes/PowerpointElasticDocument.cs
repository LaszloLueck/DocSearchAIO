using System;
using Nest;

namespace DocSearchAIO.Classes
{
    public class PowerpointElasticDocument : ElasticDocument
    {
        [Text(Name = "category")] public string Category { get; set; } = string.Empty;
        [Text(Name = "description")] public string Description { get; set; } = string.Empty;
        [Text(Name = "identifier")] public string Identifier { get; set; } = string.Empty;
        [Text(Name = "language")] public string Language { get; set; } = string.Empty;
        [Text(Name = "revision")] public string Revision { get; set; } = string.Empty;
        [Text(Name = "slideCount")] public int SlideCount { get; set; }
        [Text(Name = "version")] public string Version { get; set; } = string.Empty;
        [Text(Name = "contentStatus")] public string ContentStatus { get; set; } = string.Empty;
        [Text(Name = "lastModifiedBy")] public string LastModifiedBy { get; set; } = string.Empty;

        [Object(Name = "comments")]
        public OfficeDocumentComment[] Comments { get; set; } = Array.Empty<OfficeDocumentComment>();
        [Date(Name = "lastModified")] public DateTime Modified { get; set; }
        [Date(Name = "lastPrinted")] public DateTime LastPrinted { get; set; }
        [Date(Name = "created")] public DateTime Created { get; set; }
    }
}