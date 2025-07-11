using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Aspire.N8N.Client;

/// <summary>
/// Concrete implementation of the IN8NClient using HttpClient.
/// </summary>
public class N8NHttpClient : IN8NClient
{
    private readonly HttpClient _httpClient;

    public N8NHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/healthz", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/metrics", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> TriggerWorkflowAsync(string workflowId, object? input = null, CancellationToken cancellationToken = default)
    {
        var url = $"/webhook/{workflowId}";
        var response = await _httpClient.PostAsJsonAsync(url, input ?? new { }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}