using System.Text.Json.Serialization;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Statistics;
using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record RunnableStatistic(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("entireDocCount")]
    int EntireDocCount,
    [property: JsonPropertyName("indexedDocCount")]
    int IndexedDocCount,
    [property: JsonPropertyName("processingError")]
    int ProcessingError,
    [property: JsonPropertyName("startJob")]
    DateTime StartJob,
    [property: JsonPropertyName("endJob")] DateTime EndJob,
    [property: JsonPropertyName("elapsedTimeMillis")]
    long ElapsedTimeMillis)
{
    [JsonPropertyName("cacheEntry"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CacheEntry? CacheEntry { get; set; }

    public static implicit operator RunnableStatistic(ProcessingJobStatistic source) => new(
        source.Id, source.EntireDocCount, source.IndexedDocCount, source.ProcessingError, source.StartJob,
        source.EndJob, source.ElapsedTimeMillis
    );
}