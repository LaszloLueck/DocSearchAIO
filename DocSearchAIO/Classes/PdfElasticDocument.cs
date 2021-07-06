using Nest;

namespace DocSearchAIO.Classes
{
    public class PdfElasticDocument : ElasticDocument
    {
        [Number(Name = "pageCount")] public int PageCount { get; set; }
        
    }
}