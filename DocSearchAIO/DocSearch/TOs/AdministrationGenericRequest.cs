using System.Collections.Generic;
using DocSearchAIO.Configuration;

namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationGenericRequest
    {
        public string ScanPath { get; set; } = string.Empty;
        public List<string> ElasticEndpoints { get; set; } = new List<string>();
        public string IndexName { get; set; } = string.Empty;
        public string SchedulerName { get; set; } = string.Empty;
        public string SchedulerId { get; set; } = string.Empty;
        public string ActorSystemName { get; set; } = string.Empty;
        public string ProcessorGroupName { get; set; } = string.Empty;
        public string CleanupGroupName { get; set; } = string.Empty;
        public string UriReplacement { get; set; } = string.Empty;
        public string ComparerDirectory { get; set; } = string.Empty;
        public string StatisticsDirectory { get; set; } = string.Empty;
        public Dictionary<string, ProcessorConfiguration> ProcessorConfigurations { get; set; } = new();
        public Dictionary<string, CleanupConfiguration> CleanupConfigurations { get; set; } = new();

        public static implicit operator AdministrationGenericRequest(ConfigurationObject configurationObject) => new()
        {
            ActorSystemName = configurationObject.ActorSystemName,
            CleanupGroupName = configurationObject.CleanupGroupName,
            ComparerDirectory = configurationObject.ComparerDirectory,
            ElasticEndpoints = configurationObject.ElasticEndpoints,
            IndexName = configurationObject.IndexName,
            ProcessorGroupName = configurationObject.SchedulerGroupName,
            ScanPath = configurationObject.ScanPath,
            SchedulerId = configurationObject.SchedulerId,
            SchedulerName = configurationObject.SchedulerName,
            StatisticsDirectory = configurationObject.StatisticsDirectory,
            UriReplacement = configurationObject.UriReplacement
        };
    }

    public class ProcessorConfiguration
    {
        public int Parallelism { get; set; }
        public string ExcludeFilter { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string IndexSuffix { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string TriggerName { get; set; } = string.Empty;
        public int StartDelay { get; set; }
        public int RunsEvery { get; set; }

        public static implicit operator ProcessorConfiguration(SchedulerEntry schedulerEntry) => new()
        {
            ExcludeFilter = schedulerEntry.ExcludeFilter,
            FileExtension = schedulerEntry.FileExtension,
            IndexSuffix = schedulerEntry.IndexSuffix,
            JobName = schedulerEntry.JobName,
            Parallelism = schedulerEntry.Parallelism,
            RunsEvery = schedulerEntry.RunsEvery,
            StartDelay = schedulerEntry.StartDelay,
            TriggerName = schedulerEntry.TriggerName
        };
    }

    public class CleanupConfiguration
    {
        public string ForComparer { get; set; } = string.Empty;
        public string ForIndexSuffix { get; set; } = string.Empty;
        public int StartDelay { get; set; }
        public int RunsEvery { get; set; }
        public int Parallelism { get; set; }
        public string JobName { get; set; } = string.Empty;
        public string TriggerName { get; set; } = string.Empty;

        public static implicit operator CleanupConfiguration(CleanUpEntry source) => new()
        {
            ForComparer = source.ForComparerName,
            ForIndexSuffix = source.ForIndexSuffix,
            JobName = source.JobName,
            Parallelism = source.Parallelism,
            RunsEvery = source.RunsEvery,
            StartDelay = source.StartDelay,
            TriggerName = source.TriggerName
        };

    }
}