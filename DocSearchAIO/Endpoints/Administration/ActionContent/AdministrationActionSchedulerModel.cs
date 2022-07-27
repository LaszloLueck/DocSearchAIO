using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.ActionContent;

public record AdministrationActionSchedulerModel(string SchedulerName,
    IEnumerable<AdministrationActionTriggerModel> Triggers);