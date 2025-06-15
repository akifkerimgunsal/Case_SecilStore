using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SecilStore_ConfigLibrary.Models;
using SecilStore_ConfigLibrary.Options;
using System.Linq;
using MongoDB.Bson;

namespace SecilStore_ConfigLibrary.Repositories
{
    /// <summary>
    /// MongoDB konfigürasyon repository sınıfı
    /// </summary>
    public class MongoConfigurationRepository : IConfigurationRepository
    {
        private readonly IMongoCollection<ConfigurationEntry> _configurations;
        private readonly ConfigurationOptions _options;

        /// <summary>
        /// MongoConfigurationRepository oluşturur
        /// </summary>
        /// <param name="options">Konfigürasyon seçenekleri</param>
        public MongoConfigurationRepository(ConfigurationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            try
            {
                var client = new MongoClient(options.ConnectionString);
                var database = client.GetDatabase(options.DatabaseName);
                _configurations = database.GetCollection<ConfigurationEntry>(options.CollectionName);

                // İndeksler oluşturulur
                CreateIndexes();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB connection error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test için kullanılan constructor
        /// </summary>
        /// <param name="options">Konfigürasyon seçenekleri</param>
        /// <param name="client">MongoDB client mock</param>
        public MongoConfigurationRepository(ConfigurationOptions options, IMongoClient client)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            try
            {
                var database = client.GetDatabase(options.DatabaseName);
                _configurations = database.GetCollection<ConfigurationEntry>(options.CollectionName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB connection error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// İndeksler oluşturur
        /// </summary>
        private void CreateIndexes()
        {
            try
            {
                var indexKeysDefinition = Builders<ConfigurationEntry>.IndexKeys
                    .Ascending(c => c.ApplicationName)
                    .Ascending(c => c.Name);

                var indexOptions = new CreateIndexOptions { Unique = true };
                var indexModel = new CreateIndexModel<ConfigurationEntry>(indexKeysDefinition, indexOptions);

                _configurations.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create indexes: {ex.Message}");
                // Index oluşturma başarısız olursa devam et
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllActiveConfigurationsAsync(string applicationName)
        {
            try
            {
                var filter = Builders<ConfigurationEntry>.Filter.And(
                    Builders<ConfigurationEntry>.Filter.Eq(c => c.ApplicationName, applicationName),
                    Builders<ConfigurationEntry>.Filter.Eq(c => c.IsActive, true));

                return await _configurations.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllActiveConfigurationsAsync: {ex.Message}");
                return new List<ConfigurationEntry>();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(string applicationName)
        {
            try
            {
                var filter = Builders<ConfigurationEntry>.Filter.Eq(c => c.ApplicationName, applicationName);
                return await _configurations.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllConfigurationsAsync: {ex.Message}");
                return new List<ConfigurationEntry>();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsFromAllApplicationsAsync()
        {
            try
            {
                return await _configurations.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllConfigurationsFromAllApplicationsAsync: {ex.Message}");
                return new List<ConfigurationEntry>();
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> GetConfigurationAsync(string applicationName, string key)
        {
            try
            {
                var filter = Builders<ConfigurationEntry>.Filter.And(
                    Builders<ConfigurationEntry>.Filter.Eq(c => c.ApplicationName, applicationName),
                    Builders<ConfigurationEntry>.Filter.Eq(c => c.Name, key));

                return await _configurations.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetConfigurationAsync: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> GetConfigurationByIdAsync(string id)
        {
            try
            {
                // ObjectId geçerliliğini kontrol et
                if (!ObjectId.TryParse(id, out _))
                {
                    Console.WriteLine($"Invalid ObjectId format: {id}");
                    return null;
                }

                var filter = Builders<ConfigurationEntry>.Filter.Eq(c => c.Id, id);
                return await _configurations.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetConfigurationByIdAsync: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> AddConfigurationAsync(ConfigurationEntry configuration)
        {
            try
            {
                // Eğer Id boşsa yeni bir ObjectId oluştur
                if (string.IsNullOrEmpty(configuration.Id))
                {
                    configuration.Id = ObjectId.GenerateNewId().ToString();
                }
                else if (!ObjectId.TryParse(configuration.Id, out _))
                {
                    // Geçerli bir ObjectId değilse yeni bir tane oluştur
                    configuration.Id = ObjectId.GenerateNewId().ToString();
                }

                configuration.CreatedAt = DateTime.UtcNow;
                configuration.UpdatedAt = DateTime.UtcNow;
                await _configurations.InsertOneAsync(configuration);
                return configuration;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddConfigurationAsync: {ex.Message}");
                return configuration;
            }
        }

        /// <inheritdoc/>
        public async Task<ConfigurationEntry> UpdateConfigurationAsync(ConfigurationEntry configuration)
        {
            try
            {
                // ObjectId geçerliliğini kontrol et
                if (!ObjectId.TryParse(configuration.Id, out _))
                {
                    Console.WriteLine($"Invalid ObjectId format: {configuration.Id}");
                    return configuration;
                }

                configuration.UpdatedAt = DateTime.UtcNow;
                
                var filter = Builders<ConfigurationEntry>.Filter.Eq(c => c.Id, configuration.Id);
                await _configurations.ReplaceOneAsync(filter, configuration);
                
                return configuration;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateConfigurationAsync: {ex.Message}");
                return configuration;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteConfigurationAsync(string id)
        {
            try
            {
                // ObjectId geçerliliğini kontrol et
                if (!ObjectId.TryParse(id, out _))
                {
                    Console.WriteLine($"Invalid ObjectId format: {id}");
                    return false;
                }

                var filter = Builders<ConfigurationEntry>.Filter.Eq(c => c.Id, id);
                var result = await _configurations.DeleteOneAsync(filter);
                
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteConfigurationAsync: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ConfigurationEntry>> GetChangedConfigurationsAsync(string applicationName, DateTime lastUpdateTime)
        {
            try
            {
                var filter = Builders<ConfigurationEntry>.Filter.And(
                    Builders<ConfigurationEntry>.Filter.Eq(c => c.ApplicationName, applicationName),
                    Builders<ConfigurationEntry>.Filter.Gt(c => c.UpdatedAt, lastUpdateTime));

                return await _configurations.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetChangedConfigurationsAsync: {ex.Message}");
                return new List<ConfigurationEntry>();
            }
        }
    }
} 