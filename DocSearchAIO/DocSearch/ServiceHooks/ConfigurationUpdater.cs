using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using LanguageExt;
using LazyCache;
using MethodTimer;
using Microsoft.Extensions.Caching.Memory;

namespace DocSearchAIO.DocSearch.ServiceHooks;

public interface IConfigurationUpdater
{
    public ConfigurationObject ReadConfiguration();
    public Task UpdateConfigurationObjectAsync(ConfigurationObject configuration, bool withBackup);

    public Task<ConfigurationObject> ReadConfigurationAsync();
}

public sealed class ConfigurationUpdater : IConfigurationUpdater
{
    private readonly IConfiguration _configuration;
    //private readonly IAppCache _lazyCache;
    private readonly ILogger _logger;
    private const string ConfigCacheKey = "configurationObject";

    public ConfigurationUpdater(IConfiguration configuration, IAppCache appCache)
    {
        _configuration = configuration;
        //_lazyCache = appCache;
        _logger = LoggingFactoryBuilder.Build<ConfigurationUpdater>();
    }

    [Time]
    public ConfigurationObject ReadConfiguration()
    {
        _logger.LogInformation("try to get configuration object blocking");
        return LoadConfigurationObject();

    }

    private ConfigurationObject LoadConfigurationObject()
    {
        _logger.LogInformation("load configuration from file");
        Option<ConfigurationObject> configOpt =
            _configuration
                .GetSection(ConfigCacheKey)
                .Get<ConfigurationObject>();
        var entry = configOpt.Some(cfg => cfg).None(() =>
            throw new JsonException(
                "cannot convert configuration section to appropriate configuration object")
        );
        return entry;
    }


    [Time]
    public async Task<ConfigurationObject> ReadConfigurationAsync()
    {
        _logger.LogInformation("try to get configuration object async");
        //return await _lazyCache.GetOrAddAsync(ConfigCacheKey, async () => await Task.Run(LoadConfigurationObject));
        return await Task.Run(LoadConfigurationObject);
    }
    
    

    [Time]
    public async Task UpdateConfigurationObjectAsync(ConfigurationObject configuration, bool withBackup = false)
    {
        //_logger.LogInformation("update configuration entry for memory cache remove old cache object");
        //_lazyCache.Remove(ConfigCacheKey);

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
        [JsonPropertyName(ConfigCacheKey)]
        public ConfigurationObject ConfigurationObject { get; set; } = new();
    }
}