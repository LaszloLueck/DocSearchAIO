using System;
using Nest;

namespace DocSearchAIO.Classes
{
    public class OfficeDocumentComment
    {
        [Text(Name = "comment")] public string Comment { get; set; } = string.Empty;
        [Text(Name = "author")] public string Author { get; set; } = string.Empty;
        [Date(Name = "date")] public DateTime Date { get; set; }
        [Text(Name = "id")] public string Id { get; set; } = string.Empty;
        [Text(Name = "initials")] public string Initials { get; set; } = string.Empty;

    }
}