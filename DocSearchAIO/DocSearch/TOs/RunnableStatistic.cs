using DocSearchAIO.Scheduler;
using DocSearchAIO.Statistics;

namespace DocSearchAIO.DocSearch.TOs;

public record RunnableStatistic(string Id, int EntireDocCount, int IndexedDocCount, int ProcessingError,
    DateTime StartJob, DateTime EndJob, long ElapsedTimeMillis)
{
    public CacheEntry? CacheEntry { get; set; }

    public static implicit operator RunnableStatistic(ProcessingJobStatistic source) => new(
        source.Id, source.EntireDocCount, source.IndexedDocCount, source.ProcessingError, source.StartJob,
        source.EndJob, source.ElapsedTimeMillis
    );
}