using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace RagDoc.Web.Services.Ingestion;

/// <summary>
/// Service responsible for ingesting document data from various sources into the vector database.
/// </summary>
/// <param name="logger">The logger instance for logging operations.</param>
/// <param name="chunksCollection">The vector collection for storing document chunks.</param>
/// <param name="documentsCollection">The vector collection for storing document metadata.</param>
public class DataIngestor(
    ILogger<DataIngestor> logger,
    VectorStoreCollection<Guid, IngestedChunk> chunksCollection,
    VectorStoreCollection<Guid, IngestedDocument> documentsCollection)
{
    /// <summary>
    /// Static helper method to ingest data from a source using dependency injection.
    /// </summary>
    /// <param name="services">The service provider for dependency resolution.</param>
    /// <param name="source">The ingestion source to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task IngestDataAsync(IServiceProvider services, IIngestionSource source)
    {
        using var scope = services.CreateScope();
        var ingestor = scope.ServiceProvider.GetRequiredService<DataIngestor>();
        await ingestor.IngestDataAsync(source);
    }

    /// <summary>
    /// Ingests data from the specified source, handling document updates and deletions.
    /// </summary>
    /// <param name="source">The ingestion source to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task IngestDataAsync(IIngestionSource source)
    {
        try
        {
            await chunksCollection.EnsureCollectionExistsAsync();
            await documentsCollection.EnsureCollectionExistsAsync();

            var sourceId = source.SourceId;
            var documentsForSource = await documentsCollection.GetAsync(doc => doc.SourceId == sourceId, top: int.MaxValue).ToListAsync(System.Threading.CancellationToken.None);

            var deletedDocuments = await source.GetDeletedDocumentsAsync(documentsForSource);
            foreach (var deletedDocument in deletedDocuments)
            {
                logger.LogInformation("Removing ingested data for {documentId}", deletedDocument.DocumentId);
                await DeleteChunksForDocumentAsync(deletedDocument);
                await documentsCollection.DeleteAsync(deletedDocument.Key);
            }

            var modifiedDocuments = await source.GetNewOrModifiedDocumentsAsync(documentsForSource);
            foreach (var modifiedDocument in modifiedDocuments)
            {
                logger.LogInformation("Processing {documentId}", modifiedDocument.DocumentId);
                await DeleteChunksForDocumentAsync(modifiedDocument);

                await documentsCollection.UpsertAsync(modifiedDocument);

                var newRecords = await source.CreateChunksForDocumentAsync(modifiedDocument);
                await chunksCollection.UpsertAsync(newRecords);
            }

            logger.LogInformation("Ingestion is up-to-date");

            /// <summary>
            /// Deletes all chunks associated with the specified document.
            /// </summary>
            /// <param name="document">The document whose chunks should be deleted.</param>
            /// <returns>A task representing the asynchronous operation.</returns>
            async Task DeleteChunksForDocumentAsync(IngestedDocument document)
            {
                var documentId = document.DocumentId;
                var chunksToDelete = await chunksCollection.GetAsync(record => record.DocumentId == documentId, int.MaxValue).ToListAsync(System.Threading.CancellationToken.None);
                if (chunksToDelete.Any())
                {
                    await chunksCollection.DeleteAsync(chunksToDelete.Select(r => r.Key));
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred during data ingestion for source {sourceId}", source.SourceId);
        }
    }
}
