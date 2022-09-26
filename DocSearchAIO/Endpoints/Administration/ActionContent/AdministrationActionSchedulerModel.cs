using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.ActionContent;

[Record]
public sealed record AdministrationActionSchedulerModel(string SchedulerName,
    IEnumerable<AdministrationActionTriggerModel> Triggers);