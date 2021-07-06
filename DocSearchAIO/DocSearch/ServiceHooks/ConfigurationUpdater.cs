using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using DocSearchAIO.Utilities;
using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class ConfigurationUpdater
    {
        public static async Task UpdateConfigurationObject(ConfigurationObject configuration, bool withBackup = false)
        {
            var outer = new OuterConfigurationObject {ConfigurationObject = configuration};
            
            var str = JsonConvert.SerializeObject(outer, Formatting.Indented);
            
            withBackup.IfTrue(() => File.Copy("./Resources/config/config.json", $"./Resources/config/config_{DateTime.Now:yyyyMMddHHmmssfff}.json"));
            
            await File.WriteAllTextAsync("./Resources/config/config.json", str, Encoding.UTF8);
        }

        private class OuterConfigurationObject
        {
            [JsonProperty("configurationObject")]
            public ConfigurationObject ConfigurationObject { get; set; }
        }
    }
}