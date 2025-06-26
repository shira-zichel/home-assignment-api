using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using HomeAssignment.Configuration;
using Newtonsoft.Json;

namespace HomeAssignment.Services
{
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearAsync(string pattern = "*");
    }

    public class RedisCacheService : IRedisCacheService, IDisposable
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            IOptions<CacheSettings> cacheSettings,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _redis = redis;
            _database = redis.GetDatabase();
            _cacheSettings = cacheSettings.Value;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _distributedCache.GetStringAsync(key);
                if (cachedValue == null)
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return null;
                }

                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonConvert.DeserializeObject<T>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from Redis cache for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedValue = JsonConvert.SerializeObject(value);
                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheSettings.CacheDurationMinutes);
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options);
                _logger.LogDebug("Cached value for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in Redis cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
                _logger.LogDebug("Removed cache entry for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing value from Redis cache for key: {Key}", key);
            }
        }

        public async Task ClearAsync(string pattern = "*")
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);

                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }

                _logger.LogInformation("Cleared cache entries matching pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing Redis cache with pattern: {Pattern}", pattern);
            }
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}