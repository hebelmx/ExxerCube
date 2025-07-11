using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RagDoc.Web.Models;
using RagDoc.Web.Services.Ingestion;

/// <summary>
/// Background service that performs periodic document ingestion from configured PDF directories.
/// </summary>
/// <param name="serviceProvider">Service provider for dependency injection.</param>
/// <param name="pdfSettings">Configuration settings for PDF document processing.</param>
/// <param name="env">Web host environment information.</param>
/// <param name="logger">Logger instance for recording ingestion operations.</param>
public class IngestionWorker(
    IServiceProvider serviceProvider,
    IOptions<PdfDocumentsSettings> pdfSettings,
    IWebHostEnvironment env,
    ILogger<IngestionWorker> logger) : BackgroundService, IIngestionWorker
{
    /// <summary>
    /// Executes the document ingestion process, scanning configured directories for PDF files.
    /// </summary>
    /// <param name="stoppingToken">Token to signal cancellation of the operation.</param>
    /// <returns>A task representing the asynchronous ingestion operation.</returns>
    public async Task ExecuteIngestionAsync(CancellationToken stoppingToken)
    {
        if (pdfSettings.Value.Enabled == false)
        {
            logger.LogInformation("PDF ingestion is disabled, skipping ingestion worker execution.");
            return;
        }

        var allDirectories = new HashSet<string>(
            pdfSettings.Value.Directories ?? [],
            StringComparer.OrdinalIgnoreCase);

        var defaultDir = Path.Combine(env.WebRootPath, "Data");
        allDirectories.Add(defaultDir);

        foreach (var dir in allDirectories)
        {
            if (stoppingToken.IsCancellationRequested) break;

            if (Directory.Exists(dir))
            {
                var source = new PDFDirectorySource(dir);
                try
                {
                    await DataIngestor.IngestDataAsync(serviceProvider, source);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to ingest from {Directory}", dir);
                }
            }
            else
            {
                logger.LogWarning("PDF directory does not exist: {Directory}", dir);
            }
        }
    }

    /// <summary>
    /// Main execution method for the background service that continuously runs the ingestion process.
    /// </summary>
    /// <param name="stoppingToken">Token to signal stopping of the background service.</param>
    /// <returns>A task representing the lifetime of the background service.</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ExecuteIngestionAsync(stoppingToken);
    }
}
