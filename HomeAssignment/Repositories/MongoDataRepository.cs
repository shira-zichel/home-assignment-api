using Microsoft.Extensions.Options;
using MongoDB.Driver;
using HomeAssignment.Configuration;
using HomeAssignment.Models;

namespace HomeAssignment.Repositories
{
    public class MongoDataRepository : IDataRepository
    {
        private readonly IMongoCollection<MongoDataItem> _collection;
        private static int _nextId = 1;
        private static readonly object _lockObject = new object();

        public MongoDataRepository(IOptions<MongoDbSettings> mongoSettings)
        {
            var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);
            _collection = mongoDatabase.GetCollection<MongoDataItem>(mongoSettings.Value.CollectionName);

            // Initialize next ID based on existing data (if any)
            InitializeNextIdAsync().Wait();
        }

        private async Task InitializeNextIdAsync()
        {
            var maxId = await _collection.Find(FilterDefinition<MongoDataItem>.Empty)
                .SortByDescending(x => x.NumericId)
                .Project(x => x.NumericId)
                .FirstOrDefaultAsync();

            // If database is empty, maxId will be 0, so _nextId should be 1
            _nextId = maxId + 1;

            // Ensure _nextId is never 0
            if (_nextId <= 0)
            {
                _nextId = 1;
            }
        }

        public async Task<DataItem?> GetByIdAsync(int id)
        {
            var mongoItem = await _collection.Find(x => x.NumericId == id).FirstOrDefaultAsync();
            return mongoItem?.ToDomainModel();
        }

        public async Task<IEnumerable<DataItem>> GetAllAsync()
        {
            var mongoItems = await _collection.Find(FilterDefinition<MongoDataItem>.Empty).ToListAsync();
            return mongoItems.Select(x => x.ToDomainModel()).ToList();
        }

        public async Task<DataItem> CreateAsync(DataItem dataItem)
        {
            int assignedId;
            lock (_lockObject)
            {
                assignedId = _nextId;
                _nextId++;
            }

            dataItem.Id = assignedId;
            dataItem.CreatedAt = DateTime.UtcNow;
            var mongoItem = MongoDataItem.FromDomainModel(dataItem);

            await _collection.InsertOneAsync(mongoItem);
            return dataItem;
        }

        public async Task<DataItem?> UpdateAsync(int id, DataItem dataItem)
        {
            var filter = Builders<MongoDataItem>.Filter.Eq(x => x.NumericId, id);
            var update = Builders<MongoDataItem>.Update
                .Set(x => x.Value, dataItem.Value);

            var result = await _collection.UpdateOneAsync(filter, update);

            if (result.ModifiedCount == 0)
                return null;

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _collection.DeleteOneAsync(x => x.NumericId == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            var count = await _collection.CountDocumentsAsync(x => x.NumericId == id);
            return count > 0;
        }
    }
}