using HomeAssignment.Models;

namespace HomeAssignment.Repositories
{
    public class InMemoryDataRepository : IDataRepository
    {
        private readonly List<DataItem> _dataItems;
        private static int _nextId = 1;
        private static readonly object _lockObject = new object();

        public InMemoryDataRepository()
        {
            _dataItems = new List<DataItem>();
            // NO SAMPLE DATA - STARTS COMPLETELY EMPTY
        }

        public Task<DataItem?> GetByIdAsync(int id)
        {
            var item = _dataItems.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(item);
        }

        public Task<IEnumerable<DataItem>> GetAllAsync()
        {
            return Task.FromResult(_dataItems.AsEnumerable());
        }

        public Task<DataItem> CreateAsync(DataItem dataItem)
        {
            int assignedId;
            lock (_lockObject)
            {
                assignedId = _nextId;
                _nextId++;
            }

            dataItem.Id = assignedId;
            dataItem.CreatedAt = DateTime.UtcNow;
            _dataItems.Add(dataItem);
            return Task.FromResult(dataItem);
        }

        public Task<DataItem?> UpdateAsync(int id, DataItem dataItem)
        {
            var existingItem = _dataItems.FirstOrDefault(x => x.Id == id);
            if (existingItem == null)
                return Task.FromResult<DataItem?>(null);

            existingItem.Value = dataItem.Value;
            return Task.FromResult<DataItem?>(existingItem);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var item = _dataItems.FirstOrDefault(x => x.Id == id);
            if (item == null)
                return Task.FromResult(false);

            _dataItems.Remove(item);
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(int id)
        {
            var exists = _dataItems.Any(x => x.Id == id);
            return Task.FromResult(exists);
        }
    }
}