using Microsoft.Extensions.VectorData;

namespace RagDoc.Web.Services;

/// <summary>
/// Provides semantic search functionality for document chunks using vector similarity.
/// </summary>
/// <param name="vectorCollection">The vector collection containing document chunks for search.</param>
public class SemanticSearch(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection)
{
    /// <summary>
    /// Performs semantic search on document chunks based on the provided text query.
    /// </summary>
    /// <param name="text">The search query text.</param>
    /// <param name="documentIdFilter">Optional document ID filter to restrict search to a specific document.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>A collection of document chunks matching the search query.</returns>
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });

        return await nearest.Select(result => result.Record).ToListAsync();
    }
}
