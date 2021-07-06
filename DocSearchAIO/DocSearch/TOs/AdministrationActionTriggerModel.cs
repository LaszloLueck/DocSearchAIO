namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationActionTriggerModel
    {
        public string TriggerName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
    }
}