using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using HomeAssignment.Models;
using HomeAssignment.Services;
using Newtonsoft.Json;
using System.Text;

namespace HomeAssignment.Repositories
{
    public class CachingDataRepository : IDataRepository
    {
        private readonly IDataRepository _baseRepository;
        private readonly IFileCacheService _fileCache;
        private readonly bool _useRedis;
        private readonly int _cacheDurationMinutes;
        private readonly ILogger<CachingDataRepository> _logger;
        private readonly IDistributedCache? _distributedCache;
        private readonly IMemoryCache? _memoryCache;

        public CachingDataRepository(
            IDataRepository baseRepository,
            IFileCacheService fileCache,
            bool useRedis,
            int cacheDurationMinutes,
            ILogger<CachingDataRepository> logger,
            IDistributedCache? distributedCache = null,
            IMemoryCache? memoryCache = null)
        {
            _baseRepository = baseRepository;
            _fileCache = fileCache;
            _useRedis = useRedis;
            _cacheDurationMinutes = cacheDurationMinutes;
            _logger = logger;
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
        }

        public async Task<DataItem?> GetByIdAsync(int id)
        {
            var cacheKey = $"data_item_{id}";

            // 1. Check Cache (Redis or Memory)
            var cachedItem = await GetFromCacheAsync(cacheKey);
            if (cachedItem != null)
            {
                _logger.LogInformation("Cache HIT for ID: {Id} (Source: {CacheType})",
                    id, _useRedis ? "Redis" : "Memory");
                return cachedItem;
            }

            _logger.LogDebug("Cache MISS for ID: {Id}", id);

            // 2. Check File Storage
            var fileItem = await _fileCache.GetAsync(id);
            if (fileItem != null)
            {
                _logger.LogInformation("File cache HIT for ID: {Id}", id);

                // Store in cache for next time
                await SetInCacheAsync(cacheKey, fileItem);
                return fileItem;
            }

            _logger.LogDebug("File cache MISS for ID: {Id}", id);

            // 3. Check Database
            var dbItem = await _baseRepository.GetByIdAsync(id);
            if (dbItem != null)
            {
                _logger.LogInformation("Database HIT for ID: {Id}", id);

                // Store in both file cache and memory cache
                await _fileCache.SetAsync(id, dbItem);
                await SetInCacheAsync(cacheKey, dbItem);

                return dbItem;
            }

            _logger.LogInformation("Complete MISS for ID: {Id} - not found in any storage layer (Cache → File → Database)", id);
            return null; // This will result in 404 from controller
        }

        public async Task<IEnumerable<DataItem>> GetAllAsync()
        {
            var cacheKey = "data_items_all";

            // 1. Check Cache
            var cachedItems = await GetAllFromCacheAsync(cacheKey);
            if (cachedItems != null)
            {
                _logger.LogInformation("Cache HIT for all items (Source: {CacheType})",
                    _useRedis ? "Redis" : "Memory");
                return cachedItems;
            }

            _logger.LogDebug("Cache MISS for all items");

            // 2. Check Database (skip file cache for GetAll)
            var dbItems = await _baseRepository.GetAllAsync();

            _logger.LogInformation("Database query for all items returned {Count} items", dbItems.Count());

            // Store in cache
            await SetAllInCacheAsync(cacheKey, dbItems);

            return dbItems;
        }

        public async Task<DataItem> CreateAsync(DataItem dataItem)
        {
            var result = await _baseRepository.CreateAsync(dataItem);

            // Invalidate caches
            await InvalidateCachesAsync();

            _logger.LogInformation("Created item with ID: {Id} and invalidated caches", result.Id);
            return result;
        }

        public async Task<DataItem?> UpdateAsync(int id, DataItem dataItem)
        {
            var result = await _baseRepository.UpdateAsync(id, dataItem);

            if (result != null)
            {
                // Invalidate caches
                await InvalidateCachesAsync(id);
                _logger.LogInformation("Updated item with ID: {Id} and invalidated caches", id);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _baseRepository.DeleteAsync(id);

            if (result)
            {
                // Invalidate caches
                await InvalidateCachesAsync(id);
                _logger.LogInformation("Deleted item with ID: {Id} and invalidated caches", id);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            // For exists check, just check the database directly
            return await _baseRepository.ExistsAsync(id);
        }

        #region Cache Operations

        private async Task<DataItem?> GetFromCacheAsync(string key)
        {
            try
            {
                if (_useRedis && _distributedCache != null)
                {
                    var cachedData = await _distributedCache.GetStringAsync(key);
                    if (cachedData != null)
                    {
                        return JsonConvert.DeserializeObject<DataItem>(cachedData);
                    }
                }
                else if (_memoryCache != null)
                {
                    return _memoryCache.Get<DataItem>(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item from cache with key: {Key}", key);
            }

            return null;
        }

        private async Task SetInCacheAsync(string key, DataItem item)
        {
            try
            {
                var expiration = TimeSpan.FromMinutes(_cacheDurationMinutes);

                if (_useRedis && _distributedCache != null)
                {
                    var serializedItem = JsonConvert.SerializeObject(item);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    };
                    await _distributedCache.SetStringAsync(key, serializedItem, options);
                }
                else if (_memoryCache != null)
                {
                    _memoryCache.Set(key, item, expiration);
                }

                _logger.LogDebug("Cached item with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting item cache with key: {Key}", key);
            }
        }

        private async Task<IEnumerable<DataItem>?> GetAllFromCacheAsync(string key)
        {
            try
            {
                if (_useRedis && _distributedCache != null)
                {
                    var cachedData = await _distributedCache.GetStringAsync(key);
                    if (cachedData != null)
                    {
                        return JsonConvert.DeserializeObject<IEnumerable<DataItem>>(cachedData);
                    }
                }
                else if (_memoryCache != null)
                {
                    return _memoryCache.Get<IEnumerable<DataItem>>(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items cache with key: {Key}", key);
            }

            return null;
        }

        private async Task SetAllInCacheAsync(string key, IEnumerable<DataItem> items)
        {
            try
            {
                var expiration = TimeSpan.FromMinutes(_cacheDurationMinutes);

                if (_useRedis && _distributedCache != null)
                {
                    var serializedItems = JsonConvert.SerializeObject(items);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    };
                    await _distributedCache.SetStringAsync(key, serializedItems, options);
                }
                else if (_memoryCache != null)
                {
                    _memoryCache.Set(key, items, expiration);
                }

                _logger.LogDebug("Cached all items with key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting all items cache with key: {Key}", key);
            }
        }

        private async Task InvalidateCachesAsync(int? specificId = null)
        {
            try
            {
                // Invalidate specific item cache if ID provided
                if (specificId.HasValue)
                {
                    var itemKey = $"data_item_{specificId.Value}";
                    await RemoveFromCacheAsync(itemKey);
                    await _fileCache.RemoveAsync(specificId.Value);
                }

                // Always invalidate the "all items" cache
                var allKey = "data_items_all";
                await RemoveFromCacheAsync(allKey);

                _logger.LogDebug("Invalidated caches for ID: {Id}", specificId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating caches for ID: {Id}", specificId);
            }
        }

        private async Task RemoveFromCacheAsync(string key)
        {
            try
            {
                if (_useRedis && _distributedCache != null)
                {
                    await _distributedCache.RemoveAsync(key);
                }
                else if (_memoryCache != null)
                {
                    _memoryCache.Remove(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache with key: {Key}", key);
            }
        }

        #endregion
    }
}