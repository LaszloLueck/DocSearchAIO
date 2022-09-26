using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

[Record]
public sealed record SchedulerTriggerStatisticElement(string TriggerName, string GroupName, DateTime NextFireTime,
    DateTime StartTime, DateTime LastFireTime, string TriggerState,
    string Description, bool ProcessingState, string JobName);