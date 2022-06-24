using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

[Record]
public record SchedulerStatistics(string SchedulerName, string SchedulerInstanceId, string State)
{
    public Seq<SchedulerTriggerStatisticElement> TriggerElements { get; set; } =
        Seq<SchedulerTriggerStatisticElement>();
}