using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

public class ConfigurationUpdater : IConfigurationUpdater
{
    private readonly IConfiguration _configuration;
    private readonly IAppCache _lazyCache;
    private readonly ILogger _logger;

    public ConfigurationUpdater(IConfiguration configuration, IAppCache appCache)
    {
        _configuration = configuration;
        _lazyCache = appCache;
        _logger = LoggingFactoryBuilder.Build<ConfigurationUpdater>();
    }

    [Time]
    public ConfigurationObject ReadConfiguration()
    {
        _logger.LogInformation("try to get cached configuration object blocking");
        return _lazyCache.GetOrAdd("configurationObject", LoadConfigurationObject);
    }

    private ConfigurationObject LoadConfigurationObject(ICacheEntry cacheEntry)
    {
        _logger.LogInformation("cached object not available, try to load from file");
        Option<ConfigurationObject> configOpt =
            _configuration
                .GetSection("configurationObject")
                .Get<ConfigurationObject>();
        var entry = configOpt.Some(cfg =>
        {
            _logger.LogInformation("add configuration to cache");
            _lazyCache.Add("configurationObject", cfg);
            return cfg;
        }).None(() =>
            throw new JsonException(
                "cannot convert configuration section to appropriate configuration object")
        );
        _logger.LogInformation("add loaded configuration object to cache");
        cacheEntry.Value = entry;
        return entry;
    }

    public async Task<ConfigurationObject> ReadConfigurationAsync()
    {
        _logger.LogInformation("try to get cached configuration object blocking");
        return await _lazyCache.GetOrAddAsync("configurationObject",
            cacheEntry => Task.Run(() => LoadConfigurationObject(cacheEntry)));
    }
    
    

    [Time]
    public async Task UpdateConfigurationObjectAsync(ConfigurationObject configuration, bool withBackup = false)
    {
        _logger.LogInformation("update configuration entry for memory cache remove old cache object");
        _lazyCache.Remove("configurationObject");

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