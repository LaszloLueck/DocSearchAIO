using System.Collections.Generic;
using Newtonsoft.Json;

namespace DocSearchAIO.Configuration
{
    public class ConfigurationObject
    {
        [JsonProperty("elasticEndpoints")]
        public List<string> ElasticEndpoints { get; set; }
        
        [JsonProperty("scanPath")]
        public string ScanPath { get; set; }
        
        [JsonProperty("indexName")]
        public string IndexName { get; set; }
        
        [JsonProperty("actorSystemName")]
        public string ActorSystemName { get; set; }
        
        [JsonProperty("schedulerName")]
        public string SchedulerName { get; set; }
        
        [JsonProperty("schedulerId")]
        public string SchedulerId { get; set; }
        
        [JsonProperty("uriReplacement")]
        public string UriReplacement { get; set; }
        
        [JsonProperty("processing")]
        public Dictionary<string, SchedulerEntry> Processing { get; set; }
        
        [JsonProperty("groupName")]
        public string GroupName { get; set; }
        
        [JsonProperty("comparerDirectory")]
        public string ComparerDirectory { get; set; }
    }

    public class SchedulerEntry
    {
        [JsonProperty("active")]
        public bool Active { get; set; }
        [JsonProperty("parallelism")]
        public int Parallelism { get; set; }
        [JsonProperty("excludeFilter")]
        public string ExcludeFilter { get; set; }
        
        [JsonProperty("fileExtension")]
        public string FileExtension { get; set; }
        
        [JsonProperty("indexSuffix")]
        public string IndexSuffix { get; set; }

        [JsonProperty("jobName")]
        public string JobName { get; set; }

        [JsonProperty("triggerName")]
        public string TriggerName { get; set; }
        
        [JsonProperty("startDelay")]
        public int StartDelay { get; set; }
        
        [JsonProperty("runsEvery")]
        public int RunsEvery { get; set; }
    }
    
}