using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecilStore_ConfigLibrary.Models;

namespace SecilStore_ConfigLibrary.Cache
{
    /// <summary>
    /// Konfigürasyon cache arayüzü
    /// </summary>
    public interface IConfigurationCache
    {
        /// <summary>
        /// Tüm konfigürasyonları cache'e ekler
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="configurations">Konfigürasyonlar</param>
        /// <returns>İşlem sonucu</returns>
        Task SetAllConfigurationsAsync(string applicationName, IEnumerable<ConfigurationEntry> configurations);
        
        /// <summary>
        /// Cache'den tüm konfigürasyonları getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Konfigürasyon listesi</returns>
        Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(string applicationName);
        
        /// <summary>
        /// Cache'den belirli bir konfigürasyonu getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="key">Konfigürasyon anahtarı</param>
        /// <returns>Konfigürasyon</returns>
        Task<ConfigurationEntry> GetConfigurationAsync(string applicationName, string key);
        
        /// <summary>
        /// Cache'e bir konfigürasyon ekler veya günceller
        /// </summary>
        /// <param name="configuration">Konfigürasyon</param>
        /// <returns>İşlem sonucu</returns>
        Task SetConfigurationAsync(ConfigurationEntry configuration);
        
        /// <summary>
        /// Cache'den bir konfigürasyonu siler
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="key">Konfigürasyon anahtarı</param>
        /// <returns>İşlem sonucu</returns>
        Task<bool> RemoveConfigurationAsync(string applicationName, string key);
        
        /// <summary>
        /// Son güncelleme zamanını kaydeder
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="lastUpdateTime">Son güncelleme zamanı</param>
        /// <returns>İşlem sonucu</returns>
        Task SetLastUpdateTimeAsync(string applicationName, DateTime lastUpdateTime);
        
        /// <summary>
        /// Son güncelleme zamanını getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Son güncelleme zamanı</returns>
        Task<DateTime> GetLastUpdateTimeAsync(string applicationName);
    }
} 