using Microsoft.Extensions.VectorData;

namespace RagDoc.Web.Services;

/// <summary>
/// Represents metadata about a document that has been ingested into the system.
/// </summary>
public class IngestedDocument
{
    private const int VectorDimensions = 2;
    private const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;

    /// <summary>
    /// Gets or sets the unique identifier for this document.
    /// </summary>
    [VectorStoreKey]
    public required Guid Key { get; set; }

    /// <summary>
    /// Gets or sets the source identifier for this document.
    /// </summary>
    [VectorStoreData(IsIndexed = true)]
    public required string SourceId { get; set; }

    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    [VectorStoreData]
    public required string DocumentId { get; set; }

    /// <summary>
    /// Gets or sets the version of the document.
    /// </summary>
    [VectorStoreData]
    public required string DocumentVersion { get; set; }

    /// <summary>
    /// Gets or sets the vector representation (not used but required for some vector databases).
    /// </summary>
    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public ReadOnlyMemory<float> Vector { get; set; } = new ReadOnlyMemory<float>([0, 0]);
}
