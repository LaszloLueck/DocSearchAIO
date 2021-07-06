using System;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SchedulerTriggerStatisticElement
    {
        public string TriggerName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        
        public DateTime? NextFireTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? LastFireTime { get; set; }
        public string TriggerState { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool ProcessingState { get; set; }

        public string JobName { get; set; } = string.Empty;
        
    }
}