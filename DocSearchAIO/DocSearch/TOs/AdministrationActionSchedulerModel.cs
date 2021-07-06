using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationActionSchedulerModel
    {
        public string SchedulerName { get; set; } = string.Empty;

        public IEnumerable<AdministrationActionTriggerModel> Triggers { get; set; } =
            Array.Empty<AdministrationActionTriggerModel>();

    }
}