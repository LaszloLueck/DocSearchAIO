using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

[Record]
public sealed record SchedulerStatistics(
    [property: JsonPropertyName("schedulerName")]
    string SchedulerName,
    [property: JsonPropertyName("schedulerInstanceId")]
    string SchedulerInstanceId,
    [property: JsonPropertyName("state")] string State)
{
    public Seq<SchedulerTriggerStatisticElement> TriggerElements { get; set; } =
        Seq<SchedulerTriggerStatisticElement>();
}