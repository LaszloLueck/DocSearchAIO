using DocSearchAIO.Endpoints.Detail;
using Nest;

namespace DocSearchAIO.Classes;

public class ExcelElasticDocument : ElasticDocument
{

    [Text(Name = "category")] public string Category { get; set; } = null!;
    [Text(Name = "description")] public string Description { get; set; } = null!;
    [Text(Name = "identifier")] public string Identifier { get; set; } = null!;
    [Text(Name = "language")] public string Language { get; set; } = null!;
    [Text(Name = "revision")] public string Revision { get; set; } = null!;
    [Text(Name = "version")] public string Version { get; set; } = null!;
    [Text(Name = "contentStatus")] public string ContentStatus { get; set; } = null!;
    [Text(Name = "lastModifiedBy")] public string LastModifiedBy { get; set; } = null!;
    [Date(Name = "lastModified")] public DateTime Modified { get; set; }
    [Date(Name = "lastPrinted")] public DateTime LastPrinted { get; set; }
    [Date(Name = "created")] public DateTime Created { get; set; }
}