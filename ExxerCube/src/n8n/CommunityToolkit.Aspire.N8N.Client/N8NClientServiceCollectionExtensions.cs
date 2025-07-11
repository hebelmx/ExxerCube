//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Threading.Tasks;

//namespace ExxerAI.Aspire.Clients
//{
//    /// <summary>
//    /// Provides extension methods for registering N8N HTTP clients and configurations
//    /// via Aspire's resource discovery mechanism.
//    /// </summary>
//    public static class N8NClientServiceCollectionExtensions
//    {
//        /// <summary>
//        /// Adds a typed N8N HttpClient using the service discovery configuration.
//        /// </summary>
//        /// <param name="services">The service collection to add to.</param>
//        /// <param name="configuration">The application's configuration source.</param>
//        /// <param name="sectionKey">The manifest key for the N8N resource. Default is "n8n".</param>
//        /// <returns>The updated service collection.</returns>
//        public static IServiceCollection AddN8NClient(this IServiceCollection services, IConfiguration configuration, string sectionKey = "n8n")
//        {
//            var baseUrl = configuration[$"resources:{sectionKey}:connectionString"]
//                          ?? throw new InvalidOperationException("N8N base URL not found in manifest.");

//            services.AddHttpClient<IN8NClient, N8NHttpClient>(client =>
//            {
//                client.BaseAddress = new Uri(baseUrl);
//                client.DefaultRequestHeaders.Add("Accept", "application/json");
//            });

//            return services;
//        }
//    }

//    /// <summary>
//    /// Contract for communicating with the N8N instance.
//    /// </summary>
//    public interface IN8NClient
//    {
//        Task<string> GetHealthAsync();
//        Task<string> GetMetricsAsync();
//    }

//    /// <summary>
//    /// Concrete implementation of the IN8NClient using HttpClient.
//    /// </summary>
//    public class N8NHttpClient : IN8NClient
//    {
//        private readonly HttpClient _httpClient;

//        public N8NHttpClient(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }

//        public async Task<string> GetHealthAsync()
//        {
//            var response = await _httpClient.GetAsync("/healthz");
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        public async Task<string> GetMetricsAsync()
//        {
//            var response = await _httpClient.GetAsync("/metrics");
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }
//    }
//}

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Aspire.N8N.Client
{
    /// <summary>
    /// Provides extension methods for registering N8N HTTP clients and configurations
    /// via Aspire's resource discovery mechanism.
    /// </summary>
    public static class N8NClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a typed N8N HttpClient using the service discovery configuration.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <param name="configuration">The application's configuration source.</param>
        /// <param name="sectionKey">The manifest key for the N8N resource. Default is "n8n".</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddN8NClient(this IServiceCollection services, IConfiguration configuration, string sectionKey = "n8n")
        {
            var baseUrl = configuration[$"resources:{sectionKey}:connectionString"]
                          ?? throw new InvalidOperationException($"Connection string for resource '{sectionKey}' not found.");

            services.AddHttpClient<IN8NClient, N8NHttpClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            return services;
        }
    }
}