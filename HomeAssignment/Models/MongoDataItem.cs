using HomeAssignment.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HomeAssignment.Models
{
    public class MongoDataItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("numericId")]
        public int NumericId { get; set; }

        [BsonElement("value")]
        public string Value { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        // Convert to domain model
        public DataItem ToDomainModel()
        {
            return new DataItem
            {
                Id = NumericId,
                Value = Value,
                CreatedAt = CreatedAt
            };
        }

        // Create from domain model
        public static MongoDataItem FromDomainModel(DataItem dataItem)
        {
            return new MongoDataItem
            {
                NumericId = dataItem.Id,
                Value = dataItem.Value,
                CreatedAt = dataItem.CreatedAt
            };
        }
    }
}
