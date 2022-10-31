using DocSearchAIO.Endpoints.Detail;

namespace DocSearchAIO.Classes;

public class MsgElasticDocument : ElasticDocument
{
    public IEnumerable<string> Receiver { get; set; } = System.Array.Empty<string>();

}