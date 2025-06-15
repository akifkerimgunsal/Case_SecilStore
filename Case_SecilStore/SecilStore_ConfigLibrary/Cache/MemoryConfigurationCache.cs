using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SecilStore_ConfigLibrary.Models;
using System.Linq;

namespace SecilStore_ConfigLibrary.Cache
{
    /// <summary>
    /// Memory konfigürasyon cache sınıfı
    /// </summary>
    public class MemoryConfigurationCache : IConfigurationCache
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// MemoryConfigurationCache oluşturur
        /// </summary>
        /// <param name="cache">Memory cache</param>
        public MemoryConfigurationCache(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Konfigürasyon listesi anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Cache anahtarı</returns>
        private string GetConfigurationsKey(string applicationName)
        {
            return $"configurations:{applicationName}";
        }

        /// <summary>
        /// Konfigürasyon anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="key">Konfigürasyon anahtarı</param>
        /// <returns>Cache anahtarı</returns>
        private string GetConfigurationKey(string applicationName, string key)
        {
            return $"configuration:{applicationName}:{key}";
        }

        /// <summary>
        /// Son güncelleme zamanı anahtarını oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Cache anahtarı</returns>
        private string GetLastUpdateTimeKey(string applicationName)
        {
            return $"lastUpdateTime:{applicationName}";
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(string applicationName)
        {
            if (_cache.TryGetValue(GetConfigurationsKey(applicationName), out List<ConfigurationEntry> configurations))
            {
                return Task.FromResult<IEnumerable<ConfigurationEntry>>(configurations);
            }

            return Task.FromResult<IEnumerable<ConfigurationEntry>>(new List<ConfigurationEntry>());
        }

        /// <inheritdoc/>
        public Task<ConfigurationEntry> GetConfigurationAsync(string applicationName, string key)
        {
            if (_cache.TryGetValue(GetConfigurationKey(applicationName, key), out ConfigurationEntry configuration))
            {
                return Task.FromResult(configuration);
            }

            return Task.FromResult<ConfigurationEntry>(null);
        }

        /// <inheritdoc/>
        public Task<DateTime> GetLastUpdateTimeAsync(string applicationName)
        {
            if (_cache.TryGetValue(GetLastUpdateTimeKey(applicationName), out DateTime lastUpdateTime))
            {
                return Task.FromResult(lastUpdateTime);
            }

            return Task.FromResult(DateTime.MinValue);
        }

        /// <inheritdoc/>
        public Task<bool> RemoveConfigurationAsync(string applicationName, string key)
        {
            var configKey = GetConfigurationKey(applicationName, key);
            _cache.Remove(configKey);

            // Ayrıca listeden de kaldır
            if (_cache.TryGetValue(GetConfigurationsKey(applicationName), out List<ConfigurationEntry> configurations))
            {
                var updatedConfigurations = configurations.Where(c => c.Name != key).ToList();
                _cache.Set(GetConfigurationsKey(applicationName), updatedConfigurations, _cacheDuration);
            }

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task SetAllConfigurationsAsync(string applicationName, IEnumerable<ConfigurationEntry> configurations)
        {
            var configList = configurations.ToList();
            _cache.Set(GetConfigurationsKey(applicationName), configList, _cacheDuration);

            // Ayrıca her bir konfigürasyonu ayrı olarak da cache'e ekle
            foreach (var config in configList)
            {
                _cache.Set(GetConfigurationKey(applicationName, config.Name), config, _cacheDuration);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetConfigurationAsync(ConfigurationEntry configuration)
        {
            var configKey = GetConfigurationKey(configuration.ApplicationName, configuration.Name);
            _cache.Set(configKey, configuration, _cacheDuration);

            // Ayrıca listeyi de güncelle
            if (_cache.TryGetValue(GetConfigurationsKey(configuration.ApplicationName), out List<ConfigurationEntry> configurations))
            {
                var existingConfig = configurations.FirstOrDefault(c => c.Name == configuration.Name);
                if (existingConfig != null)
                {
                    // Mevcut konfigürasyonu güncelle
                    var index = configurations.IndexOf(existingConfig);
                    configurations[index] = configuration;
                }
                else
                {
                    // Yeni konfigürasyon ekle
                    configurations.Add(configuration);
                }

                _cache.Set(GetConfigurationsKey(configuration.ApplicationName), configurations, _cacheDuration);
            }
            else
            {
                // Yeni liste oluştur
                _cache.Set(GetConfigurationsKey(configuration.ApplicationName), new List<ConfigurationEntry> { configuration }, _cacheDuration);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetLastUpdateTimeAsync(string applicationName, DateTime lastUpdateTime)
        {
            _cache.Set(GetLastUpdateTimeKey(applicationName), lastUpdateTime, _cacheDuration);
            return Task.CompletedTask;
        }
    }
} 