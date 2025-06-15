using Microsoft.Extensions.DependencyInjection;
using SecilStore_ConfigLibrary.Cache;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Options;
using SecilStore_ConfigLibrary.Repositories;
using Microsoft.Extensions.Logging;
using SecilStore_ConfigLibrary.Messaging;

namespace SecilStore_ConfigLibrary
{
    /// <summary>
    /// Service Collection Extensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds configuration services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="options">Configuration options</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection AddConfigurationServices(this IServiceCollection services, ConfigurationOptions options)
        {
            // Repository ekle
            if (options.StorageType == StorageType.MongoDB)
            {
                services.AddSingleton<IConfigurationRepository>(sp => new MongoConfigurationRepository(options));
            }
            else
            {
                // Redis repository eklenebilir
                services.AddSingleton<IConfigurationRepository>(sp => new MongoConfigurationRepository(options));
            }
            
            // Cache ekle
            services.AddMemoryCache();
            services.AddSingleton<IConfigurationCache, MemoryConfigurationCache>();
            
            // RabbitMQ ekle (eğer kullanılacaksa)
            if (options.UseRabbitMQ)
            {
                services.AddSingleton<IMessageBroker>(sp => 
                    new RabbitMQMessageBroker(
                        options.RabbitMQConnectionString, 
                        sp.GetService<ILogger<RabbitMQMessageBroker>>()));
            }
            
            // ConfigurationReader ekle
            services.AddSingleton<IConfigurationReader>(sp => new ConfigurationReader(
                options.ApplicationName,
                options.ConnectionString,
                options.RefreshTimerIntervalInMs,
                sp.GetRequiredService<IConfigurationRepository>(),
                sp.GetRequiredService<IConfigurationCache>(),
                sp.GetService<ILogger<ConfigurationReader>>(),
                options.UseRabbitMQ ? sp.GetRequiredService<IMessageBroker>() : null));
            
            return services;
        }
    }
} 