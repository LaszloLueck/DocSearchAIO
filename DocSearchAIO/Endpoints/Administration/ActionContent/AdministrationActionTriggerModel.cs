using DocSearchAIO.Endpoints.Administration.Scheduler;
using DocSearchAIO.Scheduler;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.ActionContent;

[Record]
public sealed record AdministrationActionTriggerModel(string TriggerName, string GroupName, string CurrentState,
    string JobName)
{
    public JobState JobState { get; set; } = JobState.Undefined;
        
    public static implicit operator AdministrationActionTriggerModel(SchedulerTriggerStatisticElement statisticElement) =>
        new(statisticElement.TriggerName, statisticElement.GroupName, statisticElement.TriggerState,
            statisticElement.JobName);
}