using Microsoft.Extensions.VectorData;

namespace RagDoc.Web.Services;

/// <summary>
/// Represents a chunk of text from an ingested document that has been processed and stored in the vector database.
/// </summary>
public class IngestedChunk
{
    private const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model
    private const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;

    /// <summary>
    /// Gets or sets the unique identifier for this chunk.
    /// </summary>
    [VectorStoreKey]
    public required Guid Key { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document this chunk belongs to.
    /// </summary>
    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the page number where this chunk was found in the document.
    /// </summary>
    [VectorStoreData]
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the text content of this chunk.
    /// </summary>
    [VectorStoreData]
    public required string Text { get; set; }

    /// <summary>
    /// Gets the vector representation of the text content for semantic search.
    /// </summary>
    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public string? Vector => Text;
}
