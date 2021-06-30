using System;
using Nest;
using Newtonsoft.Json;

namespace DocSearchAIO.Classes
{
    public class OfficeDocumentComment
    {
        [Text(Name = "comment")] public string Comment { get; set; }
        [Text(Name = "author")] public string Author { get; set; }
        [Date(Name = "date")] public DateTime Date { get; set; }
        [Text(Name = "id")] public string Id { get; set; }
        [Text(Name = "initials")] public string Initials { get; set; }

    }
}