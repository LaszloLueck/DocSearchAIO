using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocSearchAIO.Configuration;

namespace DocSearchAIO.DocSearch.ServiceHooks;

public static class ConfigurationUpdater
{
    public static async Task UpdateConfigurationObject(ConfigurationObject configuration, bool withBackup = false)
    {
        var outer = new OuterConfigurationObject { ConfigurationObject = configuration };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        var str = JsonSerializer.Serialize(outer, options);

        if (withBackup)
            File.Copy("./Resources/config/config.json",
                $"./Resources/config/config_{DateTime.Now:yyyyMMddHHmmssfff}.json");

        await File.WriteAllTextAsync("./Resources/config/config.json", str, Encoding.UTF8);
    }

    private sealed class OuterConfigurationObject
    {
        [JsonPropertyName("configurationObject")] public ConfigurationObject ConfigurationObject { get; set; } = new();
    }
}