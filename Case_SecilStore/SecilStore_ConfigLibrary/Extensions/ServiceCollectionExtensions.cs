using System;
using Microsoft.Extensions.DependencyInjection;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Repositories;
using SecilStore_ConfigLibrary.Cache;
using SecilStore_ConfigLibrary.Options;

namespace SecilStore_ConfigLibrary.Extensions
{
    /// <summary>
    /// Servis koleksiyonu uzantıları
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Konfigürasyon servislerini ekler
        /// </summary>
        /// <param name="services">Servis koleksiyonu</param>
        /// <param name="options">Konfigürasyon seçenekleri</param>
        /// <returns>Servis koleksiyonu</returns>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, ConfigurationOptions options)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            // Seçenekleri ekle
            services.AddSingleton(options);
            
            // Cache'i ekle
            services.AddSingleton<IConfigurationCache, MemoryConfigurationCache>();
            
            // Repository'i ekle
            if (options.StorageType == StorageType.MongoDB)
                services.AddSingleton<IConfigurationRepository, MongoConfigurationRepository>();
            else if (options.StorageType == StorageType.Redis)
                services.AddSingleton<IConfigurationRepository, RedisConfigurationRepository>();
            else
                throw new ArgumentException($"Unknown storage type: {options.StorageType}", nameof(options.StorageType));
            
            // ConfigurationReader'ı ekle
            services.AddSingleton<IConfigurationReader, ConfigurationReader>(sp =>
            {
                var repository = sp.GetRequiredService<IConfigurationRepository>();
                var cache = sp.GetRequiredService<IConfigurationCache>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<ConfigurationReader>>();
                
                return new ConfigurationReader(
                    options.ApplicationName,
                    options.ConnectionString,
                    options.RefreshTimerIntervalInMs,
                    repository,
                    cache,
                    logger);
            });
            
            return services;
        }

        /// <summary>
        /// Konfigürasyon servislerini ekler
        /// </summary>
        /// <param name="services">Servis koleksiyonu</param>
        /// <param name="applicationName">Uygulama adı</param>
        /// <param name="connectionString">Storage bağlantı bilgileri</param>
        /// <param name="refreshTimerIntervalInMs">Ne kadar aralıklarla storage'ın kontrol edileceği bilgisi</param>
        /// <param name="storageType">Storage tipi</param>
        /// <returns>Servis koleksiyonu</returns>
        public static IServiceCollection AddConfigurationServices(
            this IServiceCollection services,
            string applicationName,
            string connectionString,
            int refreshTimerIntervalInMs,
            StorageType storageType = StorageType.MongoDB)
        {
            var options = new ConfigurationOptions
            {
                ApplicationName = applicationName,
                ConnectionString = connectionString,
                RefreshTimerIntervalInMs = refreshTimerIntervalInMs,
                StorageType = storageType
            };
            
            return services.AddConfigurationServices(options);
        }
    }
} 