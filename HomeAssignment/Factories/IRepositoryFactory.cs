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
            // Resolve the repository from DI (already wrapped with caching in Program.cs)
            return _serviceProvider.GetRequiredService<IDataRepository>();
        }

        public string GetCurrentStorageType()
        {
            return $"{_storageType} (with caching)";
        }
    }
}