using System.Threading;
using System.Threading.Tasks;

namespace CommunityToolkit.Aspire.N8N.Client;

/// <summary>
/// Contract for communicating with the N8N instance.
/// </summary>
public interface IN8NClient
{
    Task<string> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<string> GetMetricsAsync(CancellationToken cancellationToken = default);

    Task<string> TriggerWorkflowAsync(string workflowId, object? input = null, CancellationToken cancellationToken = default);
}