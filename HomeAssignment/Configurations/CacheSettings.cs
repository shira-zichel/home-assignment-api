namespace HomeAssignment.Configuration
{
    public class CacheSettings
    {
        public bool UseRedis { get; set; } = false;
        public string RedisConnectionString { get; set; } = "localhost:6379";
        public int CacheDurationMinutes { get; set; } = 10;
        public int FileCacheDurationMinutes { get; set; } = 30;
        public string FileCachePath { get; set; } = "FileCache";
    }
}
