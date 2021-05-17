using Nest;

namespace DocSearchAIO.DocSearch.Objects
{
    public class PowerpointElasticDocument : ElasticDocument
    {
        [Text(Name = "slideCount")]
        public int SlideCount { get; set; }
    }
}