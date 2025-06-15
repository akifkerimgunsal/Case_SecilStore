using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Models;

namespace SecilStore_ConfigWebUI.Services
{
    /// <summary>
    /// Konfigürasyon servisi
    /// </summary>
    public class ConfigurationService
    {
        private readonly IConfigurationReader _configurationReader;
        private readonly ILogger<ConfigurationService> _logger;

        /// <summary>
        /// ConfigurationService oluşturur
        /// </summary>
        /// <param name="configurationReader">Konfigürasyon okuyucu</param>
        /// <param name="logger">Logger</param>
        public ConfigurationService(IConfigurationReader configurationReader, ILogger<ConfigurationService> logger)
        {
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Tüm konfigürasyonları getirir
        /// </summary>
        /// <returns>Konfigürasyonlar</returns>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync()
        {
            try
            {
                return await _configurationReader.GetAllConfigurationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all configurations");
                throw;
            }
        }

        /// <summary>
        /// Belirtilen anahtarın değerini getirir
        /// </summary>
        /// <typeparam name="T">Dönüş tipi</typeparam>
        /// <param name="key">Anahtar</param>
        /// <returns>Değer</returns>
        public async Task<T> GetValueAsync<T>(string key)
        {
            try
            {
                return await _configurationReader.GetValueAsync<T>(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting value for key {key}");
                throw;
            }
        }

        /// <summary>
        /// Konfigürasyon ekler
        /// </summary>
        /// <param name="configuration">Eklenecek konfigürasyon</param>
        /// <returns>Eklenen konfigürasyon</returns>
        public async Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration)
        {
            try
            {
                return await _configurationReader.AddConfigurationAsync(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding configuration: {configuration.ApplicationName}.{configuration.Name}");
                throw;
            }
        }

        /// <summary>
        /// Konfigürasyon günceller
        /// </summary>
        /// <param name="configuration">Güncellenecek konfigürasyon</param>
        /// <returns>Güncellenen konfigürasyon</returns>
        public async Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration)
        {
            try
            {
                return await _configurationReader.UpdateConfigurationAsync(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating configuration: {configuration.ApplicationName}.{configuration.Name}");
                throw;
            }
        }

        /// <summary>
        /// Konfigürasyon siler
        /// </summary>
        /// <param name="id">Silinecek konfigürasyonun id'si</param>
        /// <returns>Başarılı ise true, değilse false</returns>
        public async Task<bool> DeleteConfigurationAsync(string id)
        {
            try
            {
                return await _configurationReader.DeleteConfigurationAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting configuration with id: {id}");
                throw;
            }
        }

        /// <summary>
        /// Konfigürasyonları yeniler
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                await _configurationReader.RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing configurations");
                throw;
            }
        }
    }
} 