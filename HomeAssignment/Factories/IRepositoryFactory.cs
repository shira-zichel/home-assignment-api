using HomeAssignment.Repositories;

namespace HomeAssignment.Factories
{
    public interface IRepositoryFactory
    {
        IDataRepository CreateRepository();
        string GetCurrentStorageType();
    }

    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _storageType;

        public RepositoryFactory(IServiceProvider serviceProvider, string storageType)
        {
            _serviceProvider = serviceProvider;
            _storageType = storageType;
        }

        public IDataRepository CreateRepository()
        {
            // Get the base repository based on storage type
            IDataRepository baseRepository = _storageType.ToLower() switch
            {
                "mongodb" => _serviceProvider.GetRequiredService<MongoDataRepository>(),
                "inmemory" => _serviceProvider.GetRequiredService<InMemoryDataRepository>(),
                _ => throw new NotSupportedException($"Storage type '{_storageType}' is not supported.")
            };

            // Always wrap with caching layer
            return _serviceProvider.GetRequiredService<CachingDataRepository>();
        }

        public string GetCurrentStorageType()
        {
            return $"{_storageType} (with caching)";
        }
    }
}