using Nest;

namespace DocSearchAIO.Classes;

public class OfficeDocumentComment
{
    [Text(Name = "comment")] public string Comment { get; set; } = null!;
    [Text(Name = "author")] public string Author { get; set; } = null!;
    [Date(Name = "date")] public DateTime Date { get; set; }
    [Text(Name = "id")] public string Id { get; set; } = null!;
    [Text(Name = "initials")] public string Initials { get; set; } = null!;
}