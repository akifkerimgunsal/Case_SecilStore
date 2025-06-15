using System;
using System.Threading;
using System.Threading.Tasks;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Repositories;
using SecilStore_ConfigLibrary.Cache;
using SecilStore_ConfigLibrary.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SecilStore_ConfigLibrary.Messaging;

namespace SecilStore_ConfigLibrary
{
    /// <summary>
    /// Konfigürasyon okuyucu ana sınıf
    /// </summary>
    public class ConfigurationReader : IConfigurationReader, IDisposable
    {
        private readonly IConfigurationRepository _repository;
        private readonly IConfigurationCache _cache;
        private readonly IMessageBroker? _messageBroker;
        private readonly string _applicationName;
        private readonly int _refreshTimerIntervalInMs;
        private readonly ILogger<ConfigurationReader>? _logger;
        private Timer? _refreshTimer;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// ConfigurationReader oluşturur
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="connectionString">Storage bağlantı bilgileri</param>
        /// <param name="refreshTimerIntervalInMs">Ne kadar aralıklarla storage'ın kontrol edileceği bilgisi</param>
        /// <param name="repository">Konfigürasyon repository</param>
        /// <param name="cache">Konfigürasyon cache</param>
        /// <param name="logger">Logger</param>
        /// <param name="messageBroker">Message broker</param>
        public ConfigurationReader(
            string applicationName,
            string connectionString,
            int refreshTimerIntervalInMs,
            IConfigurationRepository? repository = null,
            IConfigurationCache? cache = null,
            ILogger<ConfigurationReader>? logger = null,
            IMessageBroker? messageBroker = null)
        {
            if (string.IsNullOrEmpty(applicationName))
                throw new ArgumentNullException(nameof(applicationName));
            
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            
            if (refreshTimerIntervalInMs <= 0)
                throw new ArgumentException("Refresh timer interval must be greater than 0", nameof(refreshTimerIntervalInMs));
            
            _applicationName = applicationName;
            _refreshTimerIntervalInMs = refreshTimerIntervalInMs;
            _logger = logger;
            
            try
            {
                // Repository ve cache enjekte edilmemişse varsayılan olarak MongoDB ve bellek cache kullan
                var options = new Options.ConfigurationOptions
                {
                    ApplicationName = applicationName,
                    ConnectionString = connectionString,
                    RefreshTimerIntervalInMs = refreshTimerIntervalInMs
                };
                
                _repository = repository ?? new MongoConfigurationRepository(options);
                _cache = cache ?? new MemoryConfigurationCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
                _messageBroker = messageBroker;
                
                // Message broker varsa, konfigürasyon değişikliklerini dinle
                if (_messageBroker != null)
                {
                    _messageBroker.SubscribeToConfigurationChanges(applicationName, OnConfigurationChanged);
                    _logger?.LogInformation("RabbitMQ ile konfigürasyon değişiklikleri dinleniyor");
                }
                
                // Başlangıçta verileri yükle
                InitializeAsync().GetAwaiter().GetResult();
                
                // Timer'ı başlat
                StartListening();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing ConfigurationReader");
                // Repository oluşturulamadıysa, boş bir cache ile devam et
                _cache = cache ?? new MemoryConfigurationCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
                // Repository yerine null-object pattern uygulayabiliriz, ancak şimdilik sadece loglama yapıyoruz
                throw;
            }
        }

        private void OnConfigurationChanged(ConfigurationEntry configurationEntry)
        {
            _logger?.LogInformation("RabbitMQ üzerinden konfigürasyon değişikliği alındı: {ApplicationName}.{Name}", 
                configurationEntry.ApplicationName, configurationEntry.Name);
            
            // Cache'i güncelle
            _cache.SetConfigurationAsync(configurationEntry).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Başlangıçta verileri yükler
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (_isInitialized)
                    return;
                
                try
                {
                    // Aktif konfigürasyonları getir
                    var configurations = await _repository.GetAllActiveConfigurationsAsync(_applicationName);
                    
                    // Cache'e ekle
                    await _cache.SetAllConfigurationsAsync(_applicationName, configurations);
                    
                    // Son güncelleme zamanını kaydet
                    _lastUpdateTime = DateTime.UtcNow;
                    await _cache.SetLastUpdateTimeAsync(_applicationName, _lastUpdateTime);
                    
                    _isInitialized = true;
                    
                    _logger?.LogInformation($"Configuration initialized for application {_applicationName} with {configurations.Count()} items");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error initializing configuration for application {_applicationName}");
                    // Hata durumunda uygulama başlangıcını engellemiyoruz
                    _isInitialized = true; // Tekrar deneme yapılmaması için
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public T GetValue<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            
            // Asenkron metodu senkron olarak çağır
            return GetValueAsync<T>(key).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<T> GetValueAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            
            // Cache'den konfigürasyonu getir
            var configuration = await _cache.GetConfigurationAsync(_applicationName, key);
            
            // Cache'de yoksa repository'den getir
            if (configuration == null)
            {
                try
                {
                    configuration = await _repository.GetConfigurationAsync(_applicationName, key);
                    
                    // Repository'den gelen veriyi cache'e ekle
                    if (configuration != null)
                        await _cache.SetConfigurationAsync(configuration);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error getting configuration for key {key} from repository");
                    // Repository'e erişilemezse null döner, değer dönüşümünde default değer kullanılır
                }
            }
            
            // Konfigürasyon bulunamadıysa veya aktif değilse default değer döner
            if (configuration == null || !configuration.IsActive)
                return default!;
            
            // Değeri tip dönüşümü yaparak döner
            return ConvertValue<T>(configuration.Value, configuration.Type);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync()
        {
            // Cache'den tüm konfigürasyonları getir
            var configurations = await _cache.GetAllConfigurationsAsync(_applicationName);
            
            // Cache'de yoksa repository'den getir
            if (!configurations.Any())
            {
                try
                {
                    configurations = await _repository.GetAllActiveConfigurationsAsync(_applicationName);
                    
                    // Repository'den gelen verileri cache'e ekle
                    await _cache.SetAllConfigurationsAsync(_applicationName, configurations);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error getting all configurations from repository");
                    // Repository'e erişilemezse boş liste döner
                    configurations = new List<ConfigurationEntry>();
                }
            }
            
            return configurations;
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            if (configuration.ApplicationName != _applicationName)
                throw new ArgumentException($"Configuration application name does not match: {configuration.ApplicationName} != {_applicationName}");
            
            try
            {
                // Repository'de güncelle
                var updatedConfiguration = await _repository.UpdateConfigurationAsync(configuration);
                
                // Cache'i güncelle
                await _cache.SetConfigurationAsync(updatedConfiguration);
                
                // Message broker varsa, değişikliği yayınla
                _messageBroker?.PublishConfigurationChange(updatedConfiguration);
                
                _logger?.LogInformation($"Configuration updated: {updatedConfiguration.ApplicationName}.{updatedConfiguration.Name}");
                
                return updatedConfiguration;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating configuration: {configuration.ApplicationName}.{configuration.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            if (configuration.ApplicationName != _applicationName)
                throw new ArgumentException($"Configuration application name does not match: {configuration.ApplicationName} != {_applicationName}");
            
            try
            {
                // Repository'ye ekle
                var addedConfiguration = await _repository.AddConfigurationAsync(configuration);
                
                // Cache'i güncelle
                await _cache.SetConfigurationAsync(addedConfiguration);
                
                // Message broker varsa, değişikliği yayınla
                _messageBroker?.PublishConfigurationChange(addedConfiguration);
                
                _logger?.LogInformation($"Configuration added: {addedConfiguration.ApplicationName}.{addedConfiguration.Name}");
                
                return addedConfiguration;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error adding configuration: {configuration.ApplicationName}.{configuration.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteConfigurationAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));
            
            try
            {
                // Önce konfigürasyonu getir
                var configuration = await _repository.GetConfigurationByIdAsync(id);
                
                if (configuration == null || configuration.ApplicationName != _applicationName)
                    return false;
                
                // Repository'den sil
                var result = await _repository.DeleteConfigurationAsync(id);
                
                if (result)
                {
                    // Cache'den sil
                    await _cache.RemoveConfigurationAsync(_applicationName, configuration.Name);
                    
                    // Message broker varsa, değişikliği yayınla (IsActive = false olarak)
                    if (_messageBroker != null)
                    {
                        configuration.IsActive = false;
                        _messageBroker.PublishConfigurationChange(configuration);
                    }
                    
                    _logger?.LogInformation($"Configuration deleted: {configuration.ApplicationName}.{configuration.Name}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error deleting configuration with id: {id}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAsync()
        {
            try
            {
                await _semaphore.WaitAsync();
                
                // Son güncelleme zamanını getir
                _lastUpdateTime = await _cache.GetLastUpdateTimeAsync(_applicationName);
                
                try
                {
                    // Son güncellemeden sonra değişen konfigürasyonları getir
                    var changedConfigurations = await _repository.GetChangedConfigurationsAsync(_applicationName, _lastUpdateTime);
                    
                    if (changedConfigurations.Any())
                    {
                        // Değişen konfigürasyonları cache'e ekle
                        foreach (var configuration in changedConfigurations)
                        {
                            await _cache.SetConfigurationAsync(configuration);
                            
                            // Message broker varsa, değişikliği yayınla
                            _messageBroker?.PublishConfigurationChange(configuration);
                        }
                        
                        // Son güncelleme zamanını güncelle
                        _lastUpdateTime = DateTime.UtcNow;
                        await _cache.SetLastUpdateTimeAsync(_applicationName, _lastUpdateTime);
                        
                        _logger?.LogInformation($"Refreshed {changedConfigurations.Count()} configurations for application {_applicationName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error refreshing configurations for application {_applicationName}");
                    // Repository'e erişilemezse mevcut cache verilerini kullanmaya devam eder
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public void StartListening()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = new Timer(async _ => await RefreshAsync(), null, _refreshTimerIntervalInMs, _refreshTimerIntervalInMs);
            _logger?.LogInformation($"Started listening for configuration changes for application {_applicationName} with interval {_refreshTimerIntervalInMs}ms");
        }

        /// <inheritdoc/>
        public void StopListening()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            _logger?.LogInformation($"Stopped listening for configuration changes for application {_applicationName}");
        }

        /// <summary>
        /// String değeri belirtilen tipe dönüştürür
        /// </summary>
        /// <typeparam name="T">Dönüş tipi</typeparam>
        /// <param name="value">Dönüştürülecek değer</param>
        /// <param name="type">Değer tipi</param>
        /// <returns>Dönüştürülmüş değer</returns>
        private T ConvertValue<T>(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
                return default!;
            
            try
            {
                return type.ToLowerInvariant() switch
                {
                    "string" => (T)(object)value,
                    "int" => (T)(object)int.Parse(value),
                    "bool" => (T)(object)bool.Parse(value),
                    "double" => (T)(object)double.Parse(value),
                    _ => (T)(object)value
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error converting value {value} to type {type}");
                return default!;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopListening();
            _messageBroker?.StopListening();
            (_messageBroker as IDisposable)?.Dispose();
            _refreshTimer?.Dispose();
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
} 