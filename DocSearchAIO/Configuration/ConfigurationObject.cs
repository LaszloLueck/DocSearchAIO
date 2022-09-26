using System.Text.Json.Serialization;
using DocSearchAIO.Endpoints.Administration.GenericContent;

namespace DocSearchAIO.Configuration;

public class ConfigurationObject
{
    [JsonPropertyName("elasticEndpoints")] public List<string> ElasticEndpoints { get; set; } = new();

    [JsonPropertyName("elasticUser")] public string ElasticUser { get; set; } = null!;

    [JsonPropertyName("elasticPassword")] public string ElasticPassword { get; set; } = null!;

    [JsonPropertyName("scanPath")] public string ScanPath { get; set; } = null!;

    [JsonPropertyName("indexName")] public string IndexName { get; set; } = null!;

    [JsonPropertyName("actorSystemName")] public string ActorSystemName { get; set; } = null!;

    [JsonPropertyName("schedulerName")] public string SchedulerName { get; set; } = null!;

    [JsonPropertyName("schedulerId")] public string SchedulerId { get; set; } = null!;

    [JsonPropertyName("uriReplacement")] public string UriReplacement { get; set; } = null!;

    [JsonPropertyName("processing")]
    public Dictionary<string, SchedulerEntry> Processing { get; set; } = new();

    [JsonPropertyName("cleanup")]
    public Dictionary<string, CleanUpEntry> Cleanup { get; set; } = new();

    [JsonPropertyName("schedulerGroupName")] public string SchedulerGroupName { get; set; } = null!;

    [JsonPropertyName("cleanupGroupName")] public string CleanupGroupName { get; set; } = null!;

    [JsonPropertyName("comparerDirectory")] public string ComparerDirectory { get; set; } = null!;

    [JsonPropertyName("statisticsDirectory")] public string StatisticsDirectory { get; set; } = null!;
}


public class CleanUpEntry
{
    [JsonPropertyName("active")] public bool Active { get; set; }

    [JsonPropertyName("startDelay")] public int StartDelay { get; set; }

    [JsonPropertyName("runsEvery")] public int RunsEvery { get; set; }

    [JsonPropertyName("parallelism")] public int Parallelism { get; set; }

    [JsonPropertyName("jobName")] public string JobName { get; set; } = null!;

    [JsonPropertyName("triggerName")] public string TriggerName { get; set; } = null!;

    [JsonPropertyName("forComparerName")] public string ForComparerName { get; set; } = null!;

    [JsonPropertyName("forIndexSuffix")] public string ForIndexSuffix { get; set; } = null!;

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

    [JsonPropertyName("jobName")] public string JobName { get; set; } = null!;

    [JsonPropertyName("triggerName")] public string TriggerName { get; set; } = null!;

    [JsonPropertyName("excludeFilter")] public string ExcludeFilter { get; set; } = null!;

    [JsonPropertyName("fileExtension")] public string FileExtension { get; set; } = null!;

    [JsonPropertyName("indexSuffix")] public string IndexSuffix { get; set; } = null!;

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