using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public record SearchStatisticsModel(
        [property: JsonPropertyName("searchTime")] long SearchTime, 
        [property: JsonPropertyName("docCount")] long DocCount
        );
}