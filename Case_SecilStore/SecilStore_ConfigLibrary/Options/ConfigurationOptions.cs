using System;

namespace SecilStore_ConfigLibrary.Options
{
    /// <summary>
    /// Konfigürasyon seçenekleri
    /// </summary>
    public class ConfigurationOptions
    {
        /// <summary>
        /// Uygulama adı
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// Storage bağlantı bilgileri
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Ne kadar aralıklarla storage'ın kontrol edileceği bilgisi (milisaniye)
        /// </summary>
        public int RefreshTimerIntervalInMs { get; set; } = 60000; // Varsayılan: 1 dakika

        /// <summary>
        /// Kullanılacak storage tipi
        /// </summary>
        public StorageType StorageType { get; set; } = StorageType.MongoDB;

        /// <summary>
        /// Veritabanı adı
        /// </summary>
        public string DatabaseName { get; set; } = "ConfigurationDb";

        /// <summary>
        /// Koleksiyon adı
        /// </summary>
        public string CollectionName { get; set; } = "Configurations";

        /// <summary>
        /// RabbitMQ bağlantı bilgisi
        /// </summary>
        public string RabbitMQConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

        /// <summary>
        /// RabbitMQ kullanılsın mı?
        /// </summary>
        public bool UseRabbitMQ { get; set; } = false;
    }

    /// <summary>
    /// Storage tipi
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// MongoDB
        /// </summary>
        MongoDB,

        /// <summary>
        /// Redis
        /// </summary>
        Redis
    }
} 