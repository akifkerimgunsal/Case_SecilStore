using SecilStore_ConfigLibrary.Models;

namespace SecilStore_ConfigLibrary.Messaging
{
    /// <summary>
    /// Message broker arayüzü
    /// </summary>
    public interface IMessageBroker
    {
        /// <summary>
        /// Konfigürasyon değişikliğini yayınlar
        /// </summary>
        /// <param name="configurationEntry">Değişen konfigürasyon</param>
        void PublishConfigurationChange(ConfigurationEntry configurationEntry);

        /// <summary>
        /// Konfigürasyon değişikliklerini dinlemeye başlar
        /// </summary>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="onConfigurationChange">Konfigürasyon değiştiğinde çalışacak aksiyon</param>
        void SubscribeToConfigurationChanges(string applicationName, Action<ConfigurationEntry> onConfigurationChange);

        /// <summary>
        /// Dinlemeyi durdurur
        /// </summary>
        void StopListening();
    }
} 