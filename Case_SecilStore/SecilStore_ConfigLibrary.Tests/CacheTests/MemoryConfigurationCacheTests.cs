using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SecilStore_ConfigLibrary.Cache;
using SecilStore_ConfigLibrary.Models;
using Xunit;

namespace SecilStore_ConfigLibrary.Tests.CacheTests
{
    public class MemoryConfigurationCacheTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryConfigurationCache _cache;
        private readonly string _applicationName = "TestApp";

        public MemoryConfigurationCacheTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _cache = new MemoryConfigurationCache(_memoryCache);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithExistingKey_ReturnsConfiguration()
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

            await _cache.SetConfigurationAsync(configEntry);

            // Act
            var result = await _cache.GetConfigurationAsync(_applicationName, key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(configEntry.ApplicationName, result.ApplicationName);
            Assert.Equal(configEntry.Name, result.Name);
            Assert.Equal(configEntry.Value, result.Value);
        }

        [Fact]
        public async Task GetConfigurationAsync_WithNonExistingKey_ReturnsNull()
        {
            // Arrange
            var key = "NonExistentKey";

            // Act
            var result = await _cache.GetConfigurationAsync(_applicationName, key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetConfigurationAsync_AddsToCache()
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

            // Act
            await _cache.SetConfigurationAsync(configEntry);
            var result = await _cache.GetConfigurationAsync(_applicationName, configEntry.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(configEntry.ApplicationName, result.ApplicationName);
            Assert.Equal(configEntry.Name, result.Name);
            Assert.Equal(configEntry.Value, result.Value);
        }

        [Fact]
        public async Task RemoveConfigurationAsync_RemovesFromCache()
        {
            // Arrange
            var configEntry = new ConfigurationEntry
            {
                ApplicationName = _applicationName,
                Name = "KeyToRemove",
                Value = "Value",
                Type = "string",
                IsActive = true
            };

            await _cache.SetConfigurationAsync(configEntry);

            // Act
            await _cache.RemoveConfigurationAsync(_applicationName, configEntry.Name);
            var result = await _cache.GetConfigurationAsync(_applicationName, configEntry.Name);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllConfigurationsAsync_WithExistingConfigurations_ReturnsAllConfigurations()
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

            await _cache.SetAllConfigurationsAsync(_applicationName, configurations);

            // Act
            var result = await _cache.GetAllConfigurationsAsync(_applicationName);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Key1");
            Assert.Contains(result, c => c.Name == "Key2");
        }

        [Fact]
        public async Task GetAllConfigurationsAsync_WithNoConfigurations_ReturnsEmptyList()
        {
            // Arrange
            var otherAppName = "OtherApp";

            // Act
            var result = await _cache.GetAllConfigurationsAsync(otherAppName);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SetAllConfigurationsAsync_AddsAllToCache()
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

            // Act
            await _cache.SetAllConfigurationsAsync(_applicationName, configurations);
            var result = await _cache.GetAllConfigurationsAsync(_applicationName);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Key1");
            Assert.Contains(result, c => c.Name == "Key2");
        }

        [Fact]
        public async Task GetLastUpdateTimeAsync_WithExistingTime_ReturnsTime()
        {
            // Arrange
            var time = DateTime.UtcNow;
            await _cache.SetLastUpdateTimeAsync(_applicationName, time);

            // Act
            var result = await _cache.GetLastUpdateTimeAsync(_applicationName);

            // Assert
            Assert.Equal(time, result);
        }

        [Fact]
        public async Task GetLastUpdateTimeAsync_WithNoTime_ReturnsMinValue()
        {
            // Arrange
            var otherAppName = "OtherApp";

            // Act
            var result = await _cache.GetLastUpdateTimeAsync(otherAppName);

            // Assert
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public async Task SetLastUpdateTimeAsync_UpdatesTime()
        {
            // Arrange
            var time = DateTime.UtcNow;

            // Act
            await _cache.SetLastUpdateTimeAsync(_applicationName, time);
            var result = await _cache.GetLastUpdateTimeAsync(_applicationName);

            // Assert
            Assert.Equal(time, result);
        }
    }
} 