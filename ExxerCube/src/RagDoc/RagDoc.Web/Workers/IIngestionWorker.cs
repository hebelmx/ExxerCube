/// <summary>
/// Defines the contract for a background worker that performs document ingestion operations.
/// </summary>
public interface IIngestionWorker
{
    /// <summary>
    /// Executes the document ingestion process.
    /// </summary>
    /// <param name="stoppingToken">Token to signal cancellation of the operation.</param>
    /// <returns>A task representing the asynchronous ingestion operation.</returns>
    Task ExecuteIngestionAsync(CancellationToken stoppingToken);
}
