using Nest;

namespace DocSearchAIO.Classes
{
    public class PowerpointElasticDocument : ElasticDocument
    {
        [Text(Name = "slideCount")]
        public int SlideCount { get; set; }
    }
}