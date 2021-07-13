using System.Collections.Generic;
using DocSearchAIO.DocSearch.TOs;
using Newtonsoft.Json;

namespace DocSearchAIO.Configuration
{
    public class ConfigurationObject
    {
        [JsonProperty("elasticEndpoints")] public List<string> ElasticEndpoints { get; set; } = new();

        [JsonProperty("scanPath")] public string ScanPath { get; set; } = "";

        [JsonProperty("indexName")] public string IndexName { get; set; } = "";

        [JsonProperty("actorSystemName")] public string ActorSystemName { get; set; } = "";

        [JsonProperty("schedulerName")] public string SchedulerName { get; set; } = "";

        [JsonProperty("schedulerId")] public string SchedulerId { get; set; } = "";

        [JsonProperty("uriReplacement")] public string UriReplacement { get; set; } = "";

        [JsonProperty("processing")]
        public Dictionary<string, SchedulerEntry> Processing { get; set; } = new Dictionary<string, SchedulerEntry>();

        [JsonProperty("cleanup")]
        public Dictionary<string, CleanUpEntry> Cleanup { get; set; } = new Dictionary<string, CleanUpEntry>();

        [JsonProperty("schedulerGroupName")] public string SchedulerGroupName { get; set; } = "";

        [JsonProperty("cleanupGroupName")] public string CleanupGroupName { get; set; } = "";

        [JsonProperty("comparerDirectory")] public string ComparerDirectory { get; set; } = "";

        [JsonProperty("statisticsDirectory")] public string StatisticsDirectory { get; set; } = "";
    }


    public class CleanUpEntry
    {
        [JsonProperty("active")] public bool Active { get; set; }

        [JsonProperty("startDelay")] public int StartDelay { get; set; }

        [JsonProperty("runsEvery")] public int RunsEvery { get; set; }

        [JsonProperty("parallelism")] public int Parallelism { get; set; }

        [JsonProperty("jobName")] public string JobName { get; set; } = "";

        [JsonProperty("triggerName")] public string TriggerName { get; set; } = "";

        [JsonProperty("forComparerName")] public string ForComparerName { get; set; } = "";

        [JsonProperty("forIndexSuffix")] public string ForIndexSuffix { get; set; } = "";

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
        [JsonProperty("active")] public bool Active { get; set; }

        [JsonProperty("startDelay")] public int StartDelay { get; set; }

        [JsonProperty("runsEvery")] public int RunsEvery { get; set; }

        [JsonProperty("parallelism")] public int Parallelism { get; set; }

        [JsonProperty("jobName")] public string JobName { get; set; } = "";

        [JsonProperty("triggerName")] public string TriggerName { get; set; } = "";

        [JsonProperty("excludeFilter")] public string ExcludeFilter { get; set; } = "";

        [JsonProperty("fileExtension")] public string FileExtension { get; set; } = "";

        [JsonProperty("indexSuffix")] public string IndexSuffix { get; set; } = "";

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
}