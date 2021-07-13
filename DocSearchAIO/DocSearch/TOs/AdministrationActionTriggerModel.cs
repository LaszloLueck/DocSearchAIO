using DocSearchAIO.Scheduler;

namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationActionTriggerModel
    {
        public string TriggerName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public JobState JobState { get; set; } = JobState.Undefined;

        public static implicit operator AdministrationActionTriggerModel(
            SchedulerTriggerStatisticElement statisticElement) => new()
        {
            CurrentState = statisticElement.TriggerState,
            GroupName = statisticElement.GroupName,
            JobName = statisticElement.JobName,
            TriggerName = statisticElement.TriggerName
        };
    }
}