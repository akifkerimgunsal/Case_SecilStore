using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecilStore_ConfigLibrary.Models
{
    /// <summary>
    /// Konfigürasyon veri modeli
    /// </summary>
    public class ConfigurationEntry
    {
        /// <summary>
        /// Benzersiz tanımlayıcı
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// Konfigürasyon anahtarı
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Konfigürasyon değeri (string olarak saklanır, tip dönüşümü ConfigurationReader'da yapılır)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Değer tipi (string, int, bool, double)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Aktif olma durumu
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Hangi uygulamaya ait olduğu
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 