namespace DocSearchAIO.DocSearch.TOs;

public record SchedulerStatistics(string SchedulerName, string SchedulerInstanceId, string State)
{
    public IEnumerable<SchedulerTriggerStatisticElement> TriggerElements { get; set; } =
        Array.Empty<SchedulerTriggerStatisticElement>();
}