using System;
using System.Threading.Tasks;
using SecilStore_ConfigLibrary.Models;
using System.Collections.Generic;

namespace SecilStore_ConfigLibrary.Interfaces
{
    /// <summary>
    /// Konfigürasyon okuyucu arayüzü
    /// </summary>
    public interface IConfigurationReader
    {
        /// <summary>
        /// Belirtilen anahtarın değerini döner
        /// </summary>
        /// <typeparam name="T">Dönüş tipi</typeparam>
        /// <param name="key">Anahtar</param>
        /// <returns>Değer</returns>
        T GetValue<T>(string key);

        /// <summary>
        /// Belirtilen anahtarın değerini asenkron olarak döner
        /// </summary>
        /// <typeparam name="T">Dönüş tipi</typeparam>
        /// <param name="key">Anahtar</param>
        /// <returns>Değer</returns>
        Task<T> GetValueAsync<T>(string key);

        /// <summary>
        /// Tüm konfigürasyonları asenkron olarak döner
        /// </summary>
        /// <returns>Konfigürasyonlar</returns>
        Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync();

        /// <summary>
        /// Konfigürasyon ekler
        /// </summary>
        /// <param name="configuration">Eklenecek konfigürasyon</param>
        /// <returns>Eklenen konfigürasyon</returns>
        Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration);

        /// <summary>
        /// Konfigürasyon günceller
        /// </summary>
        /// <param name="configuration">Güncellenecek konfigürasyon</param>
        /// <returns>Güncellenen konfigürasyon</returns>
        Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration);

        /// <summary>
        /// Konfigürasyon siler
        /// </summary>
        /// <param name="id">Silinecek konfigürasyonun id'si</param>
        /// <returns>Başarılı ise true, değilse false</returns>
        Task<bool> DeleteConfigurationAsync(string id);

        /// <summary>
        /// Konfigürasyonları yeniler
        /// </summary>
        /// <returns>İşlem sonucu</returns>
        Task RefreshAsync();

        /// <summary>
        /// Konfigürasyon değişikliklerini dinlemeye başlar
        /// </summary>
        void StartListening();

        /// <summary>
        /// Konfigürasyon değişikliklerini dinlemeyi durdurur
        /// </summary>
        void StopListening();
    }
} 