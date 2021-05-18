using System;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SchedulerStatistics
    {
        public string TriggerName { get; set; }
        public string GroupName { get; set; }
        
        public DateTime? NextFireTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? LastFireTime { get; set; }
        public string TriggerState { get; set; }
        public string Description { get; set; }
        public bool ProcessingState { get; set; }
    }
}