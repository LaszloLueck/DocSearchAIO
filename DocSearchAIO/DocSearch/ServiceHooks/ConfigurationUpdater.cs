using System.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocSearchAIO.Configuration;
using LanguageExt;

namespace DocSearchAIO.DocSearch.ServiceHooks;

public interface IConfigurationUpdater
{
    public Task<ConfigurationObject> ReadConfigurationAsync();
    public Task UpdateConfigurationObjectAsync(ConfigurationObject configuration, bool withBackup);
}

public class ConfigurationUpdater : IConfigurationUpdater
{
    private readonly IConfiguration _configuration;

    public ConfigurationUpdater(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ConfigurationObject> ReadConfigurationAsync()
    {
        Option<string> configurationStringOpt = _configuration
            .GetSection("configurationObject")
            .Value;

        var config = configurationStringOpt.Some(cfg => cfg).None(() =>
            throw new ConfigurationErrorsException("cannot found section configurationObject"));

        Option<ConfigurationObject> configOpt = await JsonSerializer.DeserializeAsync<ConfigurationObject>(
            new MemoryStream(Encoding.UTF8.GetBytes(config)));


        return configOpt.Some(cfg => cfg).None(() =>
            throw new JsonException("cannot convert configuration section to appropriate configuration object"));
    }


    public async Task UpdateConfigurationObjectAsync(ConfigurationObject configuration, bool withBackup = false)
    {
        var outer = new OuterConfigurationObject {ConfigurationObject = configuration};
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
        [JsonPropertyName("configurationObject")]
        public ConfigurationObject ConfigurationObject { get; set; } = new();
    }
}