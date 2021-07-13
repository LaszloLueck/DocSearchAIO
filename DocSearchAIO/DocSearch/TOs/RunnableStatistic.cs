using System;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Statistics;

namespace DocSearchAIO.DocSearch.TOs
{
    public class RunnableStatistic
    {
        public string Id { get; set; } = string.Empty;
        public int EntireDocCount { get; set; }
        public int IndexedDocCount { get; set; }
        public int ProcessingError { get; set; }
        public DateTime StartJob { get; set; }
        public DateTime EndJob { get; set; }
        public long ElapsedTimeMillis { get; set; }
        public Maybe<CacheEntry> CacheEntry { get; set; }

        public static implicit operator RunnableStatistic(ProcessingJobStatistic source) => new()
        {
            Id = source.Id,
            ElapsedTimeMillis = source.ElapsedTimeMillis,
            EndJob = source.EndJob,
            EntireDocCount = source.EntireDocCount,
            IndexedDocCount = source.IndexedDocCount,
            ProcessingError = source.ProcessingError,
            StartJob = source.StartJob
        };

    }
}