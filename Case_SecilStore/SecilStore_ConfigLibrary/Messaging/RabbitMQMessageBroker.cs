using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SecilStore_ConfigLibrary.Models;

namespace SecilStore_ConfigLibrary.Messaging
{
    /// <summary>
    /// RabbitMQ implementasyonu
    /// </summary>
    public class RabbitMQMessageBroker : IMessageBroker, IDisposable
    {
        private readonly string _connectionString;
        private readonly ILogger<RabbitMQMessageBroker>? _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private EventingBasicConsumer? _consumer;
        private string? _consumerTag;
        private const string ExchangeName = "configuration_exchange";

        /// <summary>
        /// RabbitMQMessageBroker oluşturur
        /// </summary>
        /// <param name="connectionString">RabbitMQ bağlantı bilgisi</param>
        /// <param name="logger">Logger</param>
        public RabbitMQMessageBroker(string connectionString, ILogger<RabbitMQMessageBroker>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
            
            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RabbitMQ bağlantısı kurulamadı");
            }
        }

        private void Initialize()
        {
            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Topic exchange oluştur
                _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
                
                _logger?.LogInformation("RabbitMQ bağlantısı kuruldu");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "RabbitMQ initialize edilemedi");
                throw;
            }
        }

        /// <inheritdoc/>
        public void PublishConfigurationChange(ConfigurationEntry configurationEntry)
        {
            if (_channel == null || !_connection?.IsOpen == true)
            {
                _logger?.LogWarning("RabbitMQ bağlantısı kapalı, yeniden bağlanılıyor");
                try
                {
                    Initialize();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "RabbitMQ bağlantısı yeniden kurulamadı");
                    return;
                }
            }

            try
            {
                // Routing key: application.configname
                var routingKey = $"{configurationEntry.ApplicationName}.{configurationEntry.Name}";
                var message = JsonSerializer.Serialize(configurationEntry);
                var body = Encoding.UTF8.GetBytes(message);
                
                _channel?.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body);
                
                _logger?.LogInformation("Konfigürasyon değişikliği yayınlandı: {ApplicationName}.{Name}", 
                    configurationEntry.ApplicationName, configurationEntry.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon değişikliği yayınlanamadı");
            }
        }

        /// <inheritdoc/>
        public void SubscribeToConfigurationChanges(string applicationName, Action<ConfigurationEntry> onConfigurationChange)
        {
            if (_channel == null || !_connection?.IsOpen == true)
            {
                _logger?.LogWarning("RabbitMQ bağlantısı kapalı, yeniden bağlanılıyor");
                try
                {
                    Initialize();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "RabbitMQ bağlantısı yeniden kurulamadı");
                    return;
                }
            }

            try
            {
                // Geçici kuyruk oluştur
                var queueName = _channel?.QueueDeclare().QueueName;
                
                // applicationName.* routing key'i ile bind et
                var routingKey = $"{applicationName}.*";
                _channel?.QueueBind(
                    queue: queueName,
                    exchange: ExchangeName,
                    routingKey: routingKey);
                
                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += (_, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var configEntry = JsonSerializer.Deserialize<ConfigurationEntry>(message);
                        
                        if (configEntry != null && configEntry.ApplicationName == applicationName)
                        {
                            _logger?.LogInformation("Konfigürasyon değişikliği alındı: {ApplicationName}.{Name}", 
                                configEntry.ApplicationName, configEntry.Name);
                            
                            onConfigurationChange(configEntry);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Konfigürasyon değişikliği işlenemedi");
                    }
                };
                
                _consumerTag = _channel?.BasicConsume(
                    queue: queueName,
                    autoAck: true,
                    consumer: _consumer);
                
                _logger?.LogInformation("Konfigürasyon değişiklikleri dinleniyor: {ApplicationName}", applicationName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Konfigürasyon değişiklikleri dinlenemedi");
            }
        }

        /// <inheritdoc/>
        public void StopListening()
        {
            try
            {
                if (_channel != null && _consumerTag != null)
                {
                    _channel.BasicCancel(_consumerTag);
                    _consumerTag = null;
                    _logger?.LogInformation("Konfigürasyon değişiklikleri dinleme durduruldu");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Dinleme durdurulurken hata oluştu");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopListening();
            
            _channel?.Close();
            _channel?.Dispose();
            
            _connection?.Close();
            _connection?.Dispose();
            
            _logger?.LogInformation("RabbitMQ bağlantısı kapatıldı");
            
            GC.SuppressFinalize(this);
        }
    }
} 