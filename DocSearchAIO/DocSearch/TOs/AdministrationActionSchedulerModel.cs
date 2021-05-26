using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationActionSchedulerModel
    {
        public string SchedulerName { get; set; }
        public IEnumerable<AdministrationActionTriggerModel> Triggers { get; set; }
    }
}