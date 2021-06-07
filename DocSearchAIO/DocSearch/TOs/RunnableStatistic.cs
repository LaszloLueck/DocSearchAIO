using System;

namespace DocSearchAIO.DocSearch.TOs
{
    public class RunnableStatistic
    {
        public string Id { get; set; }
        public int EntireDocCount { get; set; }
        public int IndexedDocCount { get; set; }
        public int ProcessingError { get; set; }
        public DateTime StartJob { get; set; }
        public DateTime EndJob { get; set; }
        public long ElapsedTimeMillis { get; set; }
    }
}