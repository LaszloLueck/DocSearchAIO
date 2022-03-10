namespace DocSearchAIO.DocSearch.TOs
{
    public record AdministrationActionSchedulerModel(string SchedulerName,
        IEnumerable<AdministrationActionTriggerModel> Triggers);
}