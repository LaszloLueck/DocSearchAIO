using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SchedulerStatistics
    {
        public string SchedulerName { get; set; }
        public string SchedulerInstanceId { get; set; }
        public string State { get; set; }
        public IEnumerable<SchedulerTriggerStatisticElement> TriggerElements { get; set; }
    }
}