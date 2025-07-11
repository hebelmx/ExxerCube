namespace RagDoc.Web.Services.Ingestion;

/// <summary>
/// Defines the contract for data sources that can be ingested into the document vector database.
/// </summary>
public interface IIngestionSource
{
    /// <summary>
    /// Gets the unique identifier for this ingestion source.
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// Gets documents that are new or have been modified since the last ingestion.
    /// </summary>
    /// <param name="existingDocuments">List of documents that already exist in the system.</param>
    /// <returns>A collection of new or modified documents to be ingested.</returns>
    Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments);

    /// <summary>
    /// Gets documents that have been deleted from the source since the last ingestion.
    /// </summary>
    /// <param name="existingDocuments">List of documents that currently exist in the system.</param>
    /// <returns>A collection of documents that should be removed from the system.</returns>
    Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments);

    /// <summary>
    /// Creates text chunks from the specified document for vector storage.
    /// </summary>
    /// <param name="document">The document to process into chunks.</param>
    /// <returns>A collection of text chunks extracted from the document.</returns>
    Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document);
}
