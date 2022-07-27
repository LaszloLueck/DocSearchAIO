using DocSearchAIO.Configuration;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.GenericContent;

public record AdministrationGenericRequest(string ScanPath, List<string> ElasticEndpoints, string IndexName,
    string ElasticUser, string ElasticPassword,
    string SchedulerName, string SchedulerId, string ActorSystemName, string ProcessorGroupName,
    string CleanupGroupName, string UriReplacement, string ComparerDirectory, string StatisticsDirectory)
{
    public Dictionary<string, ProcessorConfiguration> ProcessorConfigurations { get; set; } = new();

    public Dictionary<string, CleanupConfiguration> CleanupConfigurations { get; set; } = new();

    public static implicit operator AdministrationGenericRequest(ConfigurationObject configurationObject) => new(
        configurationObject.ScanPath, configurationObject.ElasticEndpoints, configurationObject.IndexName,
        configurationObject.ElasticUser, configurationObject.ElasticPassword,
        configurationObject.SchedulerName, configurationObject.SchedulerId, configurationObject.ActorSystemName,
        configurationObject.SchedulerGroupName, configurationObject.CleanupGroupName,
        configurationObject.UriReplacement, configurationObject.ComparerDirectory, configurationObject
            .StatisticsDirectory);
}

public record ProcessorConfiguration(int Parallelism, int StartDelay, int RunsEvery, string ExcludeFilter,
    string IndexSuffix, string FileExtension, string JobName, string TriggerName)
{
    public static implicit operator ProcessorConfiguration(SchedulerEntry schedulerEntry) =>
        new(schedulerEntry.Parallelism, schedulerEntry.StartDelay, schedulerEntry.RunsEvery,
            schedulerEntry.ExcludeFilter, schedulerEntry.IndexSuffix, schedulerEntry.FileExtension, schedulerEntry
                .JobName, schedulerEntry.TriggerName);
}

public record CleanupConfiguration(string ForComparer, string ForIndexSuffix, int StartDelay, int RunsEvery,
    int Parallelism, string JobName, string TriggerName)
{
    public static implicit operator CleanupConfiguration(CleanUpEntry source) =>
        new(source.ForComparerName, source.ForIndexSuffix, source.StartDelay, source.RunsEvery, source.Parallelism,
            source.JobName, source.TriggerName);
}