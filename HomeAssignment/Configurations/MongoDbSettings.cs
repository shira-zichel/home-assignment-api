namespace HomeAssignment.Configuration
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "HomeAssignmentDb";
        public string CollectionName { get; set; } = "DataItems";
    }

    public class StorageSettings
    {
        public string StorageType { get; set; } = "MongoDB"; // "InMemory" or "MongoDB"
    }
}
