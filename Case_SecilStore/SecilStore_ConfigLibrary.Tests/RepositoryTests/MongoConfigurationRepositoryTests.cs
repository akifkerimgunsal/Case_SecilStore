using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Moq;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Options;
using SecilStore_ConfigLibrary.Repositories;
using Xunit;

namespace SecilStore_ConfigLibrary.Tests.RepositoryTests
{
    public class MongoConfigurationRepositoryTests
    {
        private readonly Mock<IMongoCollection<ConfigurationEntry>> _mockCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoClient> _mockClient;
        private readonly ConfigurationOptions _options;
        private readonly string _applicationName = "TestApp";

        public MongoConfigurationRepositoryTests()
        {
            _mockCollection = new Mock<IMongoCollection<ConfigurationEntry>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockClient = new Mock<IMongoClient>();
            
            _options = new ConfigurationOptions
            {
                ApplicationName = _applicationName,
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "TestDb",
                CollectionName = "TestCollection"
            };
            
            _mockDatabase.Setup(d => d.GetCollection<ConfigurationEntry>(_options.CollectionName, null))
                .Returns(_mockCollection.Object);
            _mockClient.Setup(c => c.GetDatabase(_options.DatabaseName, null))
                .Returns(_mockDatabase.Object);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithValidKey_ReturnsConfiguration()
        {
            // Arrange
            var key = "TestKey";
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = key,
                Value = "TestValue",
                Type = "string",
                IsActive = true
            };
            
            var cursor = new Mock<IAsyncCursor<ConfigurationEntry>>();
            cursor.Setup(c => c.Current).Returns(new[] { configEntry });
            cursor.SetupSequence(c => c.MoveNextAsync(default))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<FindOptions<ConfigurationEntry, ConfigurationEntry>>(),
                default))
                .ReturnsAsync(cursor.Object);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var result = await repository.GetConfigurationAsync(_applicationName, key);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(configEntry.ApplicationName, result.ApplicationName);
            Assert.Equal(configEntry.Name, result.Name);
            Assert.Equal(configEntry.Value, result.Value);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithInvalidKey_ReturnsNull()
        {
            // Arrange
            var key = "NonExistentKey";
            
            var cursor = new Mock<IAsyncCursor<ConfigurationEntry>>();
            cursor.Setup(c => c.Current).Returns(new ConfigurationEntry[] { });
            cursor.SetupSequence(c => c.MoveNextAsync(default))
                .ReturnsAsync(false);
            
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<FindOptions<ConfigurationEntry, ConfigurationEntry>>(),
                default))
                .ReturnsAsync(cursor.Object);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var result = await repository.GetConfigurationAsync(_applicationName, key);
            
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllActiveConfigurationsAsync_ReturnsActiveConfigurations()
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
            
            var cursor = new Mock<IAsyncCursor<ConfigurationEntry>>();
            cursor.Setup(c => c.Current).Returns(configurations);
            cursor.SetupSequence(c => c.MoveNextAsync(default))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<FindOptions<ConfigurationEntry, ConfigurationEntry>>(),
                default))
                .ReturnsAsync(cursor.Object);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var result = await repository.GetAllActiveConfigurationsAsync(_applicationName);
            
            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddConfigurationAsync_WithValidConfiguration_AddsAndReturnsConfiguration()
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
            
            _mockCollection.Setup(c => c.InsertOneAsync(
                It.IsAny<ConfigurationEntry>(),
                It.IsAny<InsertOneOptions>(),
                default))
                .Returns(Task.CompletedTask);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var result = await repository.AddConfigurationAsync(configEntry);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(configEntry.ApplicationName, result.ApplicationName);
            Assert.Equal(configEntry.Name, result.Name);
            Assert.Equal(configEntry.Value, result.Value);
            _mockCollection.Verify(c => c.InsertOneAsync(
                It.Is<ConfigurationEntry>(ce => ce.ApplicationName == _applicationName && ce.Name == "NewKey"),
                It.IsAny<InsertOneOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task UpdateConfigurationAsync_WithValidConfiguration_UpdatesAndReturnsConfiguration()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                Id = "507f1f77bcf86cd799439011", // Geçerli bir ObjectId formatı
                ApplicationName = _applicationName,
                Name = "ExistingKey",
                Value = "UpdatedValue",
                Type = "string",
                IsActive = true
            };
            
            _mockCollection.Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<ConfigurationEntry>(),
                It.IsAny<ReplaceOptions>(),
                default))
                .Returns(Task.FromResult((ReplaceOneResult)null));
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var updatedConfig = await repository.UpdateConfigurationAsync(configEntry);
            
            // Assert
            Assert.NotNull(updatedConfig);
            Assert.Equal(configEntry.ApplicationName, updatedConfig.ApplicationName);
            Assert.Equal(configEntry.Name, updatedConfig.Name);
            Assert.Equal(configEntry.Value, updatedConfig.Value);
            _mockCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<ConfigurationEntry>(),
                It.IsAny<ReplaceOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task DeleteConfigurationAsync_WithValidId_DeletesAndReturnsTrue()
        {
            // Arrange
            var id = "507f1f77bcf86cd799439011"; // Geçerli bir ObjectId formatı
            
            var result = new Mock<DeleteResult>();
            result.Setup(r => r.DeletedCount).Returns(1);
            
            _mockCollection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                default))
                .ReturnsAsync(result.Object);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var deleteResult = await repository.DeleteConfigurationAsync(id);
            
            // Assert
            Assert.True(deleteResult);
            _mockCollection.Verify(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task DeleteConfigurationAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var id = "invalidId"; // Geçersiz ObjectId formatı
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var deleteResult = await repository.DeleteConfigurationAsync(id);
            
            // Assert
            Assert.False(deleteResult);
            // ObjectId geçersiz olduğu için DeleteOneAsync çağrılmamalı
            _mockCollection.Verify(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                default), Times.Never);
        }

        [Fact]
        public async Task GetChangedConfigurationsAsync_ReturnsChangedConfigurations()
        {
            // Arrange
            var lastUpdateTime = DateTime.UtcNow.AddMinutes(-5);
            var configurations = new List<ConfigurationEntry>
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
            
            var cursor = new Mock<IAsyncCursor<ConfigurationEntry>>();
            cursor.Setup(c => c.Current).Returns(configurations);
            cursor.SetupSequence(c => c.MoveNextAsync(default))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ConfigurationEntry>>(),
                It.IsAny<FindOptions<ConfigurationEntry, ConfigurationEntry>>(),
                default))
                .ReturnsAsync(cursor.Object);
            
            var repository = new MongoConfigurationRepository(_options, _mockClient.Object);
            
            // Act
            var result = await repository.GetChangedConfigurationsAsync(_applicationName, lastUpdateTime);
            
            // Assert
            Assert.Equal(2, result.Count());
        }
    }
}
