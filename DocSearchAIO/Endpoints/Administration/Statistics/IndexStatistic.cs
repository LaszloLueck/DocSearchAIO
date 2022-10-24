using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.TOs;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Statistics;

[Record]
public sealed record IndexStatistic(
    [property: JsonPropertyName("indexStatisticModels")]
    IEnumerable<IndexStatisticModel> IndexStatisticModels,
    [property: JsonPropertyName("runtimeStatistics")]
    Dictionary<string, RunnableStatistic> RuntimeStatistics,
    [property: JsonPropertyName("entireDocCount")]
    long EntireDocCount,
    [property: JsonPropertyName("entireSizeInBytes")]
    double EntireSizeInBytes);