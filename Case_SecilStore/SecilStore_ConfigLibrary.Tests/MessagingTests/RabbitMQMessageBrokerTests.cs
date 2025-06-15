using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using SecilStore_ConfigLibrary.Messaging;
using SecilStore_ConfigLibrary.Models;
using Xunit;

namespace SecilStore_ConfigLibrary.Tests.MessagingTests
{
    public class RabbitMQMessageBrokerTests
    {
        private readonly Mock<ILogger<RabbitMQMessageBroker>> _mockLogger;
        private readonly string _connectionString = "amqp://guest:guest@localhost:5672";

        public RabbitMQMessageBrokerTests()
        {
            _mockLogger = new Mock<ILogger<RabbitMQMessageBroker>>();
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RabbitMQMessageBroker(null!, _mockLogger.Object));
        }

        [Fact(Skip = "Bu test gerçek RabbitMQ bağlantısı gerektirir")]
        public void PublishConfigurationChange_WithValidConfiguration_PublishesMessage()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = "TestApp",
                Name = "TestKey",
                Value = "TestValue",
                Type = "string",
                IsActive = true
            };

            var messageBroker = new RabbitMQMessageBroker(_connectionString, _mockLogger.Object);

            // Act & Assert
            // Bu test gerçek RabbitMQ bağlantısı gerektirir, bu nedenle sadece exception fırlatmamasını kontrol ediyoruz
            var exception = Record.Exception(() => messageBroker.PublishConfigurationChange(configEntry));
            Assert.Null(exception);
        }

        [Fact(Skip = "Bu test gerçek RabbitMQ bağlantısı gerektirir")]
        public void SubscribeToConfigurationChanges_WithValidCallback_SubscribesAndReceivesMessage()
        {
            // Arrange
            var applicationName = "TestApp";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = applicationName,
                Name = "TestKey",
                Value = "TestValue",
                Type = "string",
                IsActive = true
            };

            var messageBroker = new RabbitMQMessageBroker(_connectionString, _mockLogger.Object);
            var receivedConfig = (ConfigurationEntry)null!;
            var resetEvent = new ManualResetEvent(false);

            // Act
            messageBroker.SubscribeToConfigurationChanges(applicationName, config =>
            {
                receivedConfig = config;
                resetEvent.Set();
            });

            messageBroker.PublishConfigurationChange(configEntry);

            // Assert
            // Gerçek RabbitMQ bağlantısında mesajın alınması için bekle
            // Bu test gerçek RabbitMQ bağlantısı gerektirir, bu nedenle skip edilmiştir
            var signaled = resetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Assert.True(signaled);
            Assert.NotNull(receivedConfig);
            Assert.Equal(configEntry.ApplicationName, receivedConfig.ApplicationName);
            Assert.Equal(configEntry.Name, receivedConfig.Name);
            Assert.Equal(configEntry.Value, receivedConfig.Value);
        }

        [Fact(Skip = "Bu test gerçek RabbitMQ bağlantısı gerektirir")]
        public void StopListening_AfterSubscribe_StopsReceivingMessages()
        {
            // Arrange
            var applicationName = "TestApp";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = applicationName,
                Name = "TestKey",
                Value = "TestValue",
                Type = "string",
                IsActive = true
            };

            var messageBroker = new RabbitMQMessageBroker(_connectionString, _mockLogger.Object);
            var messageReceived = false;

            // Act
            messageBroker.SubscribeToConfigurationChanges(applicationName, _ => messageReceived = true);
            messageBroker.StopListening();
            messageBroker.PublishConfigurationChange(configEntry);

            // Assert
            // Gerçek RabbitMQ bağlantısında mesajın alınmaması için bekle
            // Bu test gerçek RabbitMQ bağlantısı gerektirir, bu nedenle skip edilmiştir
            Thread.Sleep(1000);
            Assert.False(messageReceived);
        }

        [Fact]
        public void Dispose_CallsDispose()
        {
            // Arrange
            // StopListening metodu virtual olmadığı için mock edilemez
            // Bu nedenle sadece Dispose metodunun exception fırlatmadığını test ediyoruz
            var messageBroker = new RabbitMQMessageBroker(_connectionString, _mockLogger.Object);

            // Act & Assert
            var exception = Record.Exception(() => messageBroker.Dispose());
            Assert.Null(exception);
        }
    }
} 