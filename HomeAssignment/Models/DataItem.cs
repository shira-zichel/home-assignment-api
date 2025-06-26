using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HomeAssignment.Models
{
    public class DataItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public int Id { get; set; }

        [BsonElement("value")]
        public string Value { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        // For API compatibility - maps MongoDB _id to integer
        [BsonIgnore]
        public int? NumericId
        {
            get
            {
                // Simple hash to convert ObjectId to integer for API compatibility
                return Math.Abs(Id.GetHashCode()) % 1000000;
            }
        }
    }
}