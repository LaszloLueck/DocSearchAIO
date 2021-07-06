using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class SchedulerStatistics
    {
        public string SchedulerName { get; set; } = string.Empty;
        public string SchedulerInstanceId { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        public IEnumerable<SchedulerTriggerStatisticElement> TriggerElements { get; set; } =
            Array.Empty<SchedulerTriggerStatisticElement>();
    }
}