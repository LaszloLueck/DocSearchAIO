using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.ActionContent;

[Record]
public record AdministrationActionSchedulerModel(string SchedulerName,
    IEnumerable<AdministrationActionTriggerModel> Triggers);