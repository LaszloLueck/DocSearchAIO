using Nest;

namespace DocSearchAIO.Classes
{
    public class PdfElasticDocument : ElasticDocument
    {
        [Text(Name = "pageCount")] public int PageCount { get; set; }
        
    }
}