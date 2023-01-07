using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record SearchStatisticsModel(
    [property: JsonPropertyName("searchTime")] long SearchTime,
    [property: JsonPropertyName("docCount")] long DocCount
);