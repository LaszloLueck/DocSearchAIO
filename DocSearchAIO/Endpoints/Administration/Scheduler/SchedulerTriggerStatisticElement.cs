using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Scheduler;

[Record]
public record SchedulerTriggerStatisticElement(string TriggerName, string GroupName, DateTime NextFireTime,
    DateTime StartTime, DateTime LastFireTime, string TriggerState,
    string Description, bool ProcessingState, string JobName);