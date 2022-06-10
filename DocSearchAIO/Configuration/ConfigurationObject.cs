using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.TOs;

namespace DocSearchAIO.Configuration;

public class ConfigurationObject
{
    [JsonPropertyName("elasticEndpoints")] public List<string> ElasticEndpoints { get; set; } = new();

    [JsonPropertyName("elasticUser")] public string ElasticUser { get; set; } = "";

    [JsonPropertyName("elasticPassword")] public string ElasticPassword { get; set; } = "";

    [JsonPropertyName("scanPath")] public string ScanPath { get; set; } = "";

    [JsonPropertyName("indexName")] public string IndexName { get; set; } = "";

    [JsonPropertyName("actorSystemName")] public string ActorSystemName { get; set; } = "";

    [JsonPropertyName("schedulerName")] public string SchedulerName { get; set; } = "";

    [JsonPropertyName("schedulerId")] public string SchedulerId { get; set; } = "";

    [JsonPropertyName("uriReplacement")] public string UriReplacement { get; set; } = "";

    [JsonPropertyName("processing")]
    public Dictionary<string, SchedulerEntry> Processing { get; set; } = new();

    [JsonPropertyName("cleanup")]
    public Dictionary<string, CleanUpEntry> Cleanup { get; set; } = new();

    [JsonPropertyName("schedulerGroupName")] public string SchedulerGroupName { get; set; } = "";

    [JsonPropertyName("cleanupGroupName")] public string CleanupGroupName { get; set; } = "";

    [JsonPropertyName("comparerDirectory")] public string ComparerDirectory { get; set; } = "";

    [JsonPropertyName("statisticsDirectory")] public string StatisticsDirectory { get; set; } = "";
}


public class CleanUpEntry
{
    [JsonPropertyName("active")] public bool Active { get; set; }

    [JsonPropertyName("startDelay")] public int StartDelay { get; set; }

    [JsonPropertyName("runsEvery")] public int RunsEvery { get; set; }

    [JsonPropertyName("parallelism")] public int Parallelism { get; set; }

    [JsonPropertyName("jobName")] public string JobName { get; set; } = "";

    [JsonPropertyName("triggerName")] public string TriggerName { get; set; } = "";

    [JsonPropertyName("forComparerName")] public string ForComparerName { get; set; } = "";

    [JsonPropertyName("forIndexSuffix")] public string ForIndexSuffix { get; set; } = "";

    public static implicit operator CleanUpEntry(CleanupConfiguration cleanupConfiguration) => new()
    {
        ForComparerName = cleanupConfiguration.ForComparer,
        ForIndexSuffix = cleanupConfiguration.ForIndexSuffix,
        JobName = cleanupConfiguration.JobName,
        Parallelism = cleanupConfiguration.Parallelism,
        RunsEvery = cleanupConfiguration.RunsEvery,
        StartDelay = cleanupConfiguration.StartDelay,
        TriggerName = cleanupConfiguration.TriggerName
    };

}

public class SchedulerEntry
{
    [JsonPropertyName("active")] public bool Active { get; set; }

    [JsonPropertyName("startDelay")] public int StartDelay { get; set; }

    [JsonPropertyName("runsEvery")] public int RunsEvery { get; set; }

    [JsonPropertyName("parallelism")] public int Parallelism { get; set; }

    [JsonPropertyName("jobName")] public string JobName { get; set; } = "";

    [JsonPropertyName("triggerName")] public string TriggerName { get; set; } = "";

    [JsonPropertyName("excludeFilter")] public string ExcludeFilter { get; set; } = "";

    [JsonPropertyName("fileExtension")] public string FileExtension { get; set; } = "";

    [JsonPropertyName("indexSuffix")] public string IndexSuffix { get; set; } = "";

    public static implicit operator SchedulerEntry(ProcessorConfiguration processorConfiguration) => new()
    {
        ExcludeFilter = processorConfiguration.ExcludeFilter,
        FileExtension = processorConfiguration.FileExtension,
        IndexSuffix = processorConfiguration.IndexSuffix,
        JobName = processorConfiguration.JobName,
        Parallelism = processorConfiguration.Parallelism,
        RunsEvery = processorConfiguration.RunsEvery,
        StartDelay = processorConfiguration.StartDelay,
        TriggerName = processorConfiguration.TriggerName
    };
}