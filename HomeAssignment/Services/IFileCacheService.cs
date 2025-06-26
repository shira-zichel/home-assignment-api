using HomeAssignment.Models;

namespace HomeAssignment.Services
{
    public interface IFileCacheService
    {
        Task<DataItem?> GetAsync(int id);
        Task<IEnumerable<DataItem>?> GetAllAsync();
        Task SetAsync(int id, DataItem item);
        Task SetAllAsync(IEnumerable<DataItem> items);
        Task RemoveAsync(int id);
        Task ClearAsync();
    }

    public class FileCacheService : IFileCacheService
    {
        private readonly string _cachePath;
        private readonly int _durationMinutes;
        private readonly ILogger<FileCacheService> _logger;

        public FileCacheService(string cachePath, int durationMinutes, ILogger<FileCacheService> logger)
        {
            _cachePath = cachePath;
            _durationMinutes = durationMinutes;
            _logger = logger;

            // Ensure cache directory exists
            Directory.CreateDirectory(_cachePath);
        }

        public async Task<DataItem?> GetAsync(int id)
        {
            try
            {
                var filePath = GetItemFilePath(id);
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("File cache miss for ID: {Id}", id);
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                if (IsExpired(fileInfo))
                {
                    _logger.LogDebug("File cache expired for ID: {Id}", id);
                    File.Delete(filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var item = Newtonsoft.Json.JsonConvert.DeserializeObject<DataItem>(json);

                _logger.LogDebug("File cache hit for ID: {Id}", id);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from file cache for ID: {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<DataItem>?> GetAllAsync()
        {
            try
            {
                var filePath = GetAllItemsFilePath();
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("File cache miss for all items");
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                if (IsExpired(fileInfo))
                {
                    _logger.LogDebug("File cache expired for all items");
                    File.Delete(filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DataItem>>(json);

                _logger.LogDebug("File cache hit for all items");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading all items from file cache");
                return null;
            }
        }

        public async Task SetAsync(int id, DataItem item)
        {
            try
            {
                var filePath = GetItemFilePath(id);
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("Saved item to file cache with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to file cache for ID: {Id}", id);
            }
        }

        public async Task SetAllAsync(IEnumerable<DataItem> items)
        {
            try
            {
                var filePath = GetAllItemsFilePath();
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(items, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("Saved all items to file cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing all items to file cache");
            }
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                var filePath = GetItemFilePath(id);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Removed item from file cache with ID: {Id}", id);
                }

                // Also invalidate the "all items" cache since it's now stale
                var allItemsPath = GetAllItemsFilePath();
                if (File.Exists(allItemsPath))
                {
                    File.Delete(allItemsPath);
                    _logger.LogDebug("Invalidated all items file cache due to item deletion");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from file cache for ID: {Id}", id);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                if (Directory.Exists(_cachePath))
                {
                    Directory.Delete(_cachePath, true);
                    Directory.CreateDirectory(_cachePath);
                }

                _logger.LogDebug("Cleared file cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing file cache");
            }
        }

        private string GetItemFilePath(int id)
        {
            var expirationTime = DateTime.UtcNow.AddMinutes(_durationMinutes);
            var timestamp = expirationTime.ToString("yyyyMMddHHmm");
            return Path.Combine(_cachePath, $"item_{id}_{timestamp}.json");
        }

        private string GetAllItemsFilePath()
        {
            var expirationTime = DateTime.UtcNow.AddMinutes(_durationMinutes);
            var timestamp = expirationTime.ToString("yyyyMMddHHmm");
            return Path.Combine(_cachePath, $"all_items_{timestamp}.json");
        }

        private bool IsExpired(FileInfo fileInfo)
        {
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var parts = fileName.Split('_');

            if (parts.Length < 3) return true;

            var timestampStr = parts[^1]; // Last part is timestamp
            if (DateTime.TryParseExact(timestampStr, "yyyyMMddHHmm", null,
                System.Globalization.DateTimeStyles.None, out var expirationTime))
            {
                return DateTime.UtcNow > expirationTime;
            }

            return true; // If we can't parse, consider it expired
        }
    }
}
