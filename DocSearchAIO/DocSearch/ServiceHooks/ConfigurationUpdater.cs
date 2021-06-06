using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using DocSearchAIO.Scheduler;
using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class ConfigurationUpdater
    {
        public static async Task UpdateConfigurationObject(ConfigurationObject configuration, bool withBackup = false)
        {
            var outer = new OuterConfigurationObject {ConfigurationObject = configuration};
            
            var str = JsonConvert.SerializeObject(outer, Formatting.Indented);
            
            if (withBackup)
            {
                File.Copy("config/config.json", $"config/config_{DateTime.Now:yyyyMMddHHmmss}.json");
            }
            
            await File.WriteAllTextAsync("config/config.json", str, Encoding.UTF8);
        }

        private class OuterConfigurationObject
        {
            [JsonProperty("configurationObject")]
            public ConfigurationObject ConfigurationObject { get; set; }
        }
    }
}