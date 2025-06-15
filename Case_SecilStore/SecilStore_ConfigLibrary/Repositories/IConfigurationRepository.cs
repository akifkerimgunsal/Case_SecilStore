using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecilStore_ConfigLibrary.Models;

namespace SecilStore_ConfigLibrary.Repositories
{
    /// <summary>
    /// Konfigürasyon repository arayüzü
    /// </summary>
    public interface IConfigurationRepository
    {
        /// <summary>
        /// Belirli bir uygulamaya ait tüm aktif konfigürasyonları getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Konfigürasyon listesi</returns>
        Task<IEnumerable<ConfigurationEntry>> GetAllActiveConfigurationsAsync(string applicationName);
        
        /// <summary>
        /// Belirli bir uygulamaya ait tüm konfigürasyonları getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <returns>Konfigürasyon listesi</returns>
        Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(string applicationName);
        
        /// <summary>
        /// Tüm uygulamalardaki tüm konfigürasyonları getirir
        /// </summary>
        /// <returns>Konfigürasyon listesi</returns>
        Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsFromAllApplicationsAsync();
        
        /// <summary>
        /// Belirli bir anahtar için konfigürasyon getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="key">Konfigürasyon anahtarı</param>
        /// <returns>Konfigürasyon</returns>
        Task<ConfigurationEntry> GetConfigurationAsync(string applicationName, string key);
        
        /// <summary>
        /// Belirli bir ID için konfigürasyon getirir
        /// </summary>
        /// <param name="id">Konfigürasyon ID'si</param>
        /// <returns>Konfigürasyon</returns>
        Task<ConfigurationEntry> GetConfigurationByIdAsync(string id);
        
        /// <summary>
        /// Yeni bir konfigürasyon ekler
        /// </summary>
        /// <param name="configuration">Eklenecek konfigürasyon</param>
        /// <returns>Eklenen konfigürasyon</returns>
        Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration);
        
        /// <summary>
        /// Bir konfigürasyonu günceller
        /// </summary>
        /// <param name="configuration">Güncellenecek konfigürasyon</param>
        /// <returns>Güncellenen konfigürasyon</returns>
        Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration);
        
        /// <summary>
        /// Bir konfigürasyonu siler
        /// </summary>
        /// <param name="id">Silinecek konfigürasyonun ID'si</param>
        /// <returns>İşlem başarılı mı</returns>
        Task<bool> DeleteConfigurationAsync(string id);
        
        /// <summary>
        /// Son güncelleme zamanından sonra değişen konfigürasyonları getirir
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="lastUpdateTime">Son güncelleme zamanı</param>
        /// <returns>Değişen konfigürasyon listesi</returns>
        Task<IEnumerable<ConfigurationEntry>> GetChangedConfigurationsAsync(string applicationName, DateTime lastUpdateTime);
    }
} 