using System;
using System.Collections.Generic;
using Nest;
using Newtonsoft.Json;

namespace DocSearchAIO.Classes
{
    public class WordElasticDocument : ElasticDocument
    {
        [Text(Name = "version")] public string Version { get; set; }
        [Text(Name = "contentStatus")] public string ContentStatus { get; set; }
        [Text(Name = "category")] public string Category { get; set; }
        [Text(Name = "description")] public string Description { get; set; }
        [Text(Name = "identifier")] public string Identifier { get; set; }
        [Text(Name = "language")] public string Language { get; set; }
        [Text(Name = "revision")] public string Revision { get; set; }
        [Text(Name = "lastModifiedBy")] public string LastModifiedBy { get; set; }
        [Object(Name = "comments")] public OfficeDocumentComment[] Comments { get; set; }
        [Date(Name = "lastModified")] public DateTime Modified { get; set; }
        [Date(Name = "lastPrinted")] public DateTime LastPrinted { get; set; }
        [Date(Name = "created")] public DateTime Created { get; set; }
    }
}