using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SecilStore_ConfigLibrary.Cache;
using SecilStore_ConfigLibrary.Interfaces;
using SecilStore_ConfigLibrary.Messaging;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Repositories;
using Xunit;

namespace SecilStore_ConfigLibrary.Tests
{
    public class ConfigurationReaderTests
    {
        private readonly Mock<IConfigurationRepository> _mockRepository;
        private readonly Mock<IConfigurationCache> _mockCache;
        private readonly Mock<ILogger<ConfigurationReader>> _mockLogger;
        private readonly Mock<IMessageBroker> _mockMessageBroker;
        private readonly string _applicationName = "TestApp";
        private readonly string _connectionString = "mongodb://localhost:27017";
        private readonly int _refreshInterval = 60000;

        public ConfigurationReaderTests()
        {
            _mockRepository = new Mock<IConfigurationRepository>();
            _mockCache = new Mock<IConfigurationCache>();
            _mockLogger = new Mock<ILogger<ConfigurationReader>>();
            _mockMessageBroker = new Mock<IMessageBroker>();
        }

        [Fact]
        public void Constructor_WithNullApplicationName_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConfigurationReader(
                null!,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ConfigurationReader(
                _applicationName,
                null!,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithInvalidRefreshInterval_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new ConfigurationReader(
                _applicationName,
                _connectionString,
                0,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object));
        }

        [Fact]
        public async Task GetValueAsync_WithValidKey_ReturnsValue()
        {
            // Arrange
            var key = "TestKey";
            var expectedValue = "TestValue";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = key,
                Value = expectedValue,
                Type = "string",
                IsActive = true
            };

            _mockCache.Setup(c => c.GetConfigurationAsync(_applicationName, key))
                .ReturnsAsync(configEntry);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await reader.GetValueAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
            _mockCache.Verify(c => c.GetConfigurationAsync(_applicationName, key), Times.Once);
            _mockRepository.Verify(r => r.GetConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetValueAsync_WithCacheMiss_FetchesFromRepository()
        {
            // Arrange
            var key = "TestKey";
            var expectedValue = "TestValue";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = key,
                Value = expectedValue,
                Type = "string",
                IsActive = true
            };

            _mockCache.Setup(c => c.GetConfigurationAsync(_applicationName, key))
                .ReturnsAsync((ConfigurationEntry)null!);
            _mockRepository.Setup(r => r.GetConfigurationAsync(_applicationName, key))
                .ReturnsAsync(configEntry);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await reader.GetValueAsync<string>(key);

            // Assert
            Assert.Equal(expectedValue, result);
            _mockCache.Verify(c => c.GetConfigurationAsync(_applicationName, key), Times.Once);
            _mockRepository.Verify(r => r.GetConfigurationAsync(_applicationName, key), Times.Once);
            _mockCache.Verify(c => c.SetConfigurationAsync(configEntry), Times.Once);
        }

        [Fact]
        public async Task GetValueAsync_WithInactiveConfiguration_ReturnsDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = key,
                Value = "TestValue",
                Type = "string",
                IsActive = false
            };

            _mockCache.Setup(c => c.GetConfigurationAsync(_applicationName, key))
                .ReturnsAsync(configEntry);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await reader.GetValueAsync<string>(key);

            // Assert
            Assert.Null(result);
            _mockCache.Verify(c => c.GetConfigurationAsync(_applicationName, key), Times.Once);
        }

        [Fact]
        public async Task GetAllConfigurationsAsync_WithCacheHit_ReturnsCachedConfigurations()
        {
            // Arrange
            var configurations = new List<ConfigurationEntry>
            {
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "Key1",
                    Value = "Value1",
                    Type = "string",
                    IsActive = true
                },
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "Key2",
                    Value = "Value2",
                    Type = "string",
                    IsActive = true
                }
            };

            _mockCache.Setup(c => c.GetAllConfigurationsAsync(_applicationName))
                .ReturnsAsync(configurations);

            _mockRepository.Setup(r => r.GetAllActiveConfigurationsAsync(_applicationName))
                .ReturnsAsync(configurations);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await reader.GetAllConfigurationsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            _mockCache.Verify(c => c.GetAllConfigurationsAsync(_applicationName), Times.Once);
        }

        [Fact]
        public async Task GetAllConfigurationsAsync_WithCacheMiss_FetchesFromRepository()
        {
            // Arrange
            var configurations = new List<ConfigurationEntry>
            {
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "Key1",
                    Value = "Value1",
                    Type = "string",
                    IsActive = true
                },
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "Key2",
                    Value = "Value2",
                    Type = "string",
                    IsActive = true
                }
            };

            _mockCache.Setup(c => c.GetAllConfigurationsAsync(_applicationName))
                .ReturnsAsync(new List<ConfigurationEntry>());
            
            _mockRepository.Setup(r => r.GetAllActiveConfigurationsAsync(_applicationName))
                .ReturnsAsync(configurations);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object);

            // Act
            var result = await reader.GetAllConfigurationsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            _mockCache.Verify(c => c.GetAllConfigurationsAsync(_applicationName), Times.Once);
            _mockCache.Verify(c => c.SetAllConfigurationsAsync(_applicationName, configurations), Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddConfigurationAsync_WithValidConfiguration_AddsAndPublishes()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = "NewKey",
                Value = "NewValue",
                Type = "string",
                IsActive = true
            };

            _mockRepository.Setup(r => r.AddConfigurationAsync(configEntry))
                .ReturnsAsync(configEntry);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Act
            var result = await reader.AddConfigurationAsync(configEntry);

            // Assert
            Assert.Equal(configEntry, result);
            _mockRepository.Verify(r => r.AddConfigurationAsync(configEntry), Times.Once);
            _mockCache.Verify(c => c.SetConfigurationAsync(configEntry), Times.Once);
            _mockMessageBroker.Verify(m => m.PublishConfigurationChange(configEntry), Times.Once);
        }

        [Fact]
        public async Task UpdateConfigurationAsync_WithValidConfiguration_UpdatesAndPublishes()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = "ExistingKey",
                Value = "UpdatedValue",
                Type = "string",
                IsActive = true
            };

            _mockRepository.Setup(r => r.UpdateConfigurationAsync(configEntry))
                .ReturnsAsync(configEntry);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Act
            var result = await reader.UpdateConfigurationAsync(configEntry);

            // Assert
            Assert.Equal(configEntry, result);
            _mockRepository.Verify(r => r.UpdateConfigurationAsync(configEntry), Times.Once);
            _mockCache.Verify(c => c.SetConfigurationAsync(configEntry), Times.Once);
            _mockMessageBroker.Verify(m => m.PublishConfigurationChange(configEntry), Times.Once);
        }

        [Fact]
        public async Task DeleteConfigurationAsync_WithValidId_DeletesAndPublishes()
        {
            // Arrange
            var id = "123";
            var configEntry = new ConfigurationEntry
            {
                Id = id,
                ApplicationName = _applicationName,
                Name = "KeyToDelete",
                Value = "Value",
                Type = "string",
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetConfigurationByIdAsync(id))
                .ReturnsAsync(configEntry);
            _mockRepository.Setup(r => r.DeleteConfigurationAsync(id))
                .ReturnsAsync(true);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Act
            var result = await reader.DeleteConfigurationAsync(id);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.GetConfigurationByIdAsync(id), Times.Once);
            _mockRepository.Verify(r => r.DeleteConfigurationAsync(id), Times.Once);
            _mockCache.Verify(c => c.RemoveConfigurationAsync(_applicationName, configEntry.Name), Times.Once);
            _mockMessageBroker.Verify(m => m.PublishConfigurationChange(It.Is<ConfigurationEntry>(ce => 
                ce.Id == id && ce.IsActive == false)), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WithChangedConfigurations_UpdatesCacheAndPublishes()
        {
            // Arrange
            var lastUpdateTime = DateTime.UtcNow.AddMinutes(-5);
            var changedConfigurations = new List<ConfigurationEntry>
            {
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "ChangedKey1",
                    Value = "ChangedValue1",
                    Type = "string",
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                },
                new ConfigurationEntry
                {
                    ApplicationName = _applicationName,
                    Name = "ChangedKey2",
                    Value = "ChangedValue2",
                    Type = "string",
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockRepository.Setup(r => r.GetAllActiveConfigurationsAsync(_applicationName))
                .ReturnsAsync(new List<ConfigurationEntry>());

            _mockCache.Setup(c => c.GetLastUpdateTimeAsync(_applicationName))
                .ReturnsAsync(lastUpdateTime);
            _mockRepository.Setup(r => r.GetChangedConfigurationsAsync(_applicationName, lastUpdateTime))
                .ReturnsAsync(changedConfigurations);

            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Act
            await reader.RefreshAsync();

            // Assert
            _mockCache.Verify(c => c.GetLastUpdateTimeAsync(_applicationName), Times.Once);
            _mockRepository.Verify(r => r.GetChangedConfigurationsAsync(_applicationName, lastUpdateTime), Times.Once);
            _mockCache.Verify(c => c.SetConfigurationAsync(It.IsAny<ConfigurationEntry>()), Times.Exactly(2));
            _mockMessageBroker.Verify(m => m.PublishConfigurationChange(It.IsAny<ConfigurationEntry>()), Times.Exactly(2));
            _mockCache.Verify(c => c.SetLastUpdateTimeAsync(_applicationName, It.IsAny<DateTime>()), Times.AtLeastOnce);
        }

        [Fact]
        public void OnConfigurationChanged_WithValidConfiguration_UpdatesCache()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = "ChangedKey",
                Value = "ChangedValue",
                Type = "string",
                IsActive = true
            };

            _mockMessageBroker.Setup(m => m.SubscribeToConfigurationChanges(
                _applicationName, It.IsAny<Action<ConfigurationEntry>>()))
                .Callback<string, Action<ConfigurationEntry>>((app, callback) => callback(configEntry));

            // Act
            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Assert
            _mockMessageBroker.Verify(m => m.SubscribeToConfigurationChanges(
                _applicationName, It.IsAny<Action<ConfigurationEntry>>()), Times.Once);
            _mockCache.Verify(c => c.SetConfigurationAsync(configEntry), Times.Once);
        }

        [Fact]
        public void Dispose_CallsStopListeningOnMessageBroker()
        {
            // Arrange
            var reader = new ConfigurationReader(
                _applicationName,
                _connectionString,
                _refreshInterval,
                _mockRepository.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockMessageBroker.Object);

            // Act
            reader.Dispose();

            // Assert
            _mockMessageBroker.Verify(m => m.StopListening(), Times.Once);
        }
    }
} 