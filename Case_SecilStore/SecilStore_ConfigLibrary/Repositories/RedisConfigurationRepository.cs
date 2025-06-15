using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Options;
using System.Linq;
using System.Text.Json;

namespace SecilStore_ConfigLibrary.Repositories
{
    /// <summary>
    /// Redis konfigürasyon repository sınıfı
    /// </summary>
    public class RedisConfigurationRepository : IConfigurationRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly Options.ConfigurationOptions _options;

        /// <summary>
        /// RedisConfigurationRepository oluşturur
        /// </summary>
        /// <param name="options">Konfigürasyon seçenekleri</param>
        public RedisConfigurationRepository(Options.ConfigurationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _redis = ConnectionMultiplexer.Connect(options.ConnectionString);
            _database = _redis.GetDatabase();
        }

        /// <summary>
        /// Konfigürasyon anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="key">Konfigürasyon anahtarı</param>
        /// <returns>Redis anahtarı</returns>
        private string GetConfigurationKey(string applicationName, string key)
        {
            return $"config:{applicationName}:{key}";
        }

        /// <summary>
        /// Uygulama anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Redis anahtarı</returns>
        private string GetApplicationKey(string applicationName)
        {
            return $"app:{applicationName}";
        }

        /// <summary>
        /// Son güncelleme zamanı anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Redis anahtarı</returns>
        private string GetLastUpdateTimeKey(string applicationName)
        {
            return $"lastUpdate:{applicationName}";
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllActiveConfigurationsAsync(string applicationName)
        {
            var allConfigurations = await GetAllConfigurationsAsync(applicationName);
            return allConfigurations.Where(c => c.IsActive);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(string applicationName)
        {
            var appKey = GetApplicationKey(applicationName);
            var configKeys = await _database.SetMembersAsync(appKey);
            
            var result = new List<ConfigurationEntry>();
            
            foreach (var configKey in configKeys)
            {
                var configJson = await _database.StringGetAsync(configKey.ToString());
                if (!configJson.IsNullOrEmpty)
                {
                    var config = JsonSerializer.Deserialize<ConfigurationEntry>(configJson);
                    result.Add(config);
                }
            }
            
            return result;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsFromAllApplicationsAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var appKeys = server.Keys(pattern: "app:*");
            
            var result = new List<ConfigurationEntry>();
            
            foreach (var appKey in appKeys)
            {
                var configKeys = await _database.SetMembersAsync(appKey);
                
                foreach (var configKey in configKeys)
                {
                    var configJson = await _database.StringGetAsync(configKey.ToString());
                    if (!configJson.IsNullOrEmpty)
                    {
                        var config = JsonSerializer.Deserialize<ConfigurationEntry>(configJson);
                        result.Add(config);
                    }
                }
            }
            
            return result;
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> GetConfigurationAsync(string applicationName, string key)
        {
            var configKey = GetConfigurationKey(applicationName, key);
            var configJson = await _database.StringGetAsync(configKey);
            
            if (configJson.IsNullOrEmpty)
                return null;
                
            var config = JsonSerializer.Deserialize<ConfigurationEntry>(configJson);
            
            return config.IsActive ? config : null;
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> GetConfigurationByIdAsync(string id)
        {
            var allConfigs = await GetAllConfigurationsFromAllApplicationsAsync();
            return allConfigs.FirstOrDefault(c => c.Id == id);
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration)
        {
            if (string.IsNullOrEmpty(configuration.Id))
                configuration.Id = Guid.NewGuid().ToString();
                
            configuration.CreatedAt = DateTime.UtcNow;
            configuration.UpdatedAt = DateTime.UtcNow;
            
            var configKey = GetConfigurationKey(configuration.ApplicationName, configuration.Name);
            var appKey = GetApplicationKey(configuration.ApplicationName);
            
            var configJson = JsonSerializer.Serialize(configuration);
            
            await _database.StringSetAsync(configKey, configJson);
            await _database.SetAddAsync(appKey, configKey);
            await UpdateLastUpdateTimeAsync(configuration.ApplicationName);
            
            return configuration;
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration)
        {
            configuration.UpdatedAt = DateTime.UtcNow;
            
            var configKey = GetConfigurationKey(configuration.ApplicationName, configuration.Name);
            var configJson = JsonSerializer.Serialize(configuration);
            
            await _database.StringSetAsync(configKey, configJson);
            await UpdateLastUpdateTimeAsync(configuration.ApplicationName);
            
            return configuration;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteConfigurationAsync(string id)
        {
            var allConfigs = await GetAllConfigurationsFromAllApplicationsAsync();
            var config = allConfigs.FirstOrDefault(c => c.Id == id);
            
            if (config == null)
                return false;
                
            var configKey = GetConfigurationKey(config.ApplicationName, config.Name);
            var appKey = GetApplicationKey(config.ApplicationName);
            
            await _database.KeyDeleteAsync(configKey);
            await _database.SetRemoveAsync(appKey, configKey);
            await UpdateLastUpdateTimeAsync(config.ApplicationName);
            
            return true;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetChangedConfigurationsAsync(string applicationName, DateTime lastUpdateTime)
        {
            var allConfigurations = await GetAllConfigurationsAsync(applicationName);
            return allConfigurations.Where(c => c.UpdatedAt > lastUpdateTime);
        }

        /// <summary>
        /// Son güncelleme zamanını günceller
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>İşlem sonucu</returns>
        private async Task UpdateLastUpdateTimeAsync(string applicationName)
        {
            var lastUpdateKey = GetLastUpdateTimeKey(applicationName);
            await _database.StringSetAsync(lastUpdateKey, DateTime.UtcNow.Ticks.ToString());
        }
    }
} 