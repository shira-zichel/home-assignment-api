using Polly;
using Polly.Extensions.Http;
using HomeAssignment.Services;

namespace HomeAssignment.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Polly resilience patterns (Retry, Circuit Breaker, Timeout)
        /// </summary>
        public static IServiceCollection AddPollyPolicies(this IServiceCollection services)
        {
            // Add HTTP clients with Polly policies
            services.AddHttpClient("ExternalApiClient")
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy())
                .AddPolicyHandler(GetTimeoutPolicy());

            // Register the external API service that uses these policies
            services.AddScoped<IExternalApiService, ExternalApiService>();

            Console.WriteLine("✅ Polly resilience patterns configured");
            return services;
        }

        /// <summary>
        /// Retry policy: 3 attempts with exponential backoff
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles HttpRequestException and 5XX, 408 status codes
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"🔄 Polly Retry: Attempt {retryCount} after {timespan}s delay");
                    });
        }

        /// <summary>
        /// Circuit breaker: Opens after 3 failures, stays open for 30 seconds
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, timespan) =>
                    {
                        Console.WriteLine($"⚡ Polly Circuit Breaker: Opened for {timespan}s");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("✅ Polly Circuit Breaker: Reset (closed)");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("🟡 Polly Circuit Breaker: Half-open (testing)");
                    });
        }

        /// <summary>
        /// Timeout policy: Cancel requests after 10 seconds
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(10); // 10 second timeout
        }
    }
}
