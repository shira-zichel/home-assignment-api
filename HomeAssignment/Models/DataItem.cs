using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HomeAssignment.Models
{
    public class DataItem
    {
        public int Id { get; set; }

        public string Value { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}