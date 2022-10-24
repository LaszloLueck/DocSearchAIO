using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

[Record]
public sealed record SchedulerTriggerStatisticElement(
    [property: JsonPropertyName("triggerName")]
    string TriggerName,
    [property: JsonPropertyName("groupName")]
    string GroupName,
    [property: JsonPropertyName("nextFireTime")]
    DateTime NextFireTime,
    [property: JsonPropertyName("startTime")]
    DateTime StartTime,
    [property: JsonPropertyName("lastFireTime")]
    DateTime LastFireTime,
    [property: JsonPropertyName("triggerState")]
    string TriggerState,
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("processingState")]
    bool ProcessingState,
    [property: JsonPropertyName("jobName")]
    string JobName);