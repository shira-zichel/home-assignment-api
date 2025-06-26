using Polly.Timeout;
using System.Text.Json;

namespace HomeAssignment.Services
{
    public interface IExternalApiService
    {
        Task<string> GetExternalDataAsync(string endpoint);
        Task<T?> GetExternalDataAsync<T>(string endpoint) where T : class;
        Task<bool> PostDataAsync<T>(string endpoint, T data) where T : class;
        Task<bool> TestPollyResilienceAsync();
    }

    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiService> _logger;

        public ExternalApiService(IHttpClientFactory httpClientFactory, ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ExternalApiClient");
            _logger = logger;
        }

        /// <summary>
        /// Get data from external API with Polly resilience (retry, circuit breaker, timeout)
        /// </summary>
        public async Task<string> GetExternalDataAsync(string endpoint)
        {
            try
            {
                _logger.LogInformation("🌐 Calling external API: {Endpoint}", endpoint);

                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ Successfully retrieved data from external API");

                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ HTTP error calling external API: {Endpoint}", endpoint);
                throw;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutRejectedException)
            {
                _logger.LogError(ex, "⏱️ Timeout calling external API: {Endpoint}", endpoint);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error calling external API: {Endpoint}", endpoint);
                throw;
            }
        }

        /// <summary>
        /// Get typed data from external API with JSON deserialization
        /// </summary>
        public async Task<T?> GetExternalDataAsync<T>(string endpoint) where T : class
        {
            try
            {
                var jsonString = await GetExternalDataAsync(endpoint);
                return JsonSerializer.Deserialize<T>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "📄 JSON deserialization error for type {Type}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// Post data to external API with Polly resilience
        /// </summary>
        public async Task<bool> PostDataAsync<T>(string endpoint, T data) where T : class
        {
            try
            {
                _logger.LogInformation("📤 Posting data to external API: {Endpoint}", endpoint);

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("✅ Successfully posted data to external API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error posting data to external API: {Endpoint}", endpoint);
                return false;
            }
        }

        /// <summary>
        /// Test Polly resilience patterns with a sample API call
        /// </summary>
        public async Task<bool> TestPollyResilienceAsync()
        {
            try
            {
                _logger.LogInformation("🧪 Testing Polly resilience patterns...");

                // Test with a reliable public API
                var testEndpoint = "https://httpbin.org/get";
                var result = await GetExternalDataAsync(testEndpoint);

                _logger.LogInformation("✅ Polly resilience test completed successfully");
                return !string.IsNullOrEmpty(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Polly resilience test failed");
                return false;
            }
        }
    }

    // Example DTOs for external API responses
    public class ExternalApiResponse
    {
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class WeatherData
    {
        public string? Location { get; set; }
        public double Temperature { get; set; }
        public string? Description { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}