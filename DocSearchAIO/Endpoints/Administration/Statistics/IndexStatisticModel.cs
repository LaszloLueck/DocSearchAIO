using System.Text.Json.Serialization;
using LanguageExt;
using Nest;

namespace DocSearchAIO.Endpoints.Administration.Statistics;

[Record]
public sealed record IndexStatisticModel(
    [property: JsonPropertyName("indexName")]
    string IndexName, 
    [property: JsonPropertyName("docCount")]
    long DocCount, 
    [property: JsonPropertyName("sizeInBytes")]
    double SizeInBytes, 
    [property: JsonPropertyName("fetchTimeMs")]
    long FetchTimeMs,
    [property: JsonPropertyName("fetchTotal")]
    long FetchTotal, 
    [property: JsonPropertyName("queryTimeMs")]
    long QueryTimeMs, 
    [property: JsonPropertyName("queryTotal")]
    long QueryTotal, 
    [property: JsonPropertyName("suggestTimeMs")]
    long SuggestTimeMs, 
    [property: JsonPropertyName("suggestTotal")]
    long SuggestTotal)
{
    public static explicit operator IndexStatisticModel(IndicesStatsResponse response) =>
        new(response.Indices.First().Key, response.Stats.Total.Documents.Count, response.Stats.Total.Store
                .SizeInBytes,
            response.Stats.Total.Search.FetchTimeInMilliseconds, response.Stats.Total.Search.FetchTotal, response
                .Stats.Total.Search.QueryTimeInMilliseconds, response.Stats.Total.Search.QueryTotal,
            response.Stats.Total.Search.SuggestTimeInMilliseconds, response.Stats.Total.Search.SuggestTotal);
}