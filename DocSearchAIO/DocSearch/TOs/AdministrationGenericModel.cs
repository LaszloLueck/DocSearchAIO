using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class AdministrationGenericModel
    {
        public string ScanPath { get; set; }
        public List<string> ElasticEndpoints { get; set; }
        public string IndexName { get; set; }
        public string SchedulerName { get; set; }
        public string SchedulerId { get; set; }
        public string ActorSystemName { get; set; }
        public string GroupName { get; set; }
        public string UriReplacement { get; set; }

        public IEnumerable<ProcessorConfiguration> ProcessorConfigurations { get; set; }
        public class ProcessorConfiguration
        {
            public string ProcessorType { get; set; }
            public int Parallelism { get; set; }
            public string ExcludeFilter { get; set; }
            public string FileExtension { get; set; }
            public string IndexSuffix { get; set; }
            public string JobName { get; set; }
            public string TriggerName { get; set; }
            public int StartDelay { get; set; }
            public int RunsEvery { get; set; }
        }
        
    }
}