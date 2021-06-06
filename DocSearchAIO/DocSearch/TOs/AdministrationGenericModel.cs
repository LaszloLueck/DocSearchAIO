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
    }
}