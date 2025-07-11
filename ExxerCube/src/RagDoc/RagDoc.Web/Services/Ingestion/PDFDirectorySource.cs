using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace RagDoc.Web.Services.Ingestion;

/// <summary>
/// Ingestion source that monitors a directory for PDF files and extracts their content for vector storage.
/// </summary>
/// <param name="sourceDirectory">The directory to monitor for PDF files.</param>
public class PDFDirectorySource(string sourceDirectory) : IIngestionSource
{
    /// <summary>
    /// Extracts the file ID from a file path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file name without directory information.</returns>
    public static string SourceFileId(string path) => Path.GetFileName(path);

    /// <summary>
    /// Gets the version string for a file based on its last write time.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The file version as an ISO 8601 formatted timestamp.</returns>
    public static string SourceFileVersion(string path) => File.GetLastWriteTimeUtc(path).ToString("o");

    /// <summary>
    /// Gets the unique identifier for this PDF directory source.
    /// </summary>
    public string SourceId => $"{nameof(PDFDirectorySource)}:{sourceDirectory}";

    /// <summary>
    /// Gets PDF documents that are new or have been modified since the last ingestion.
    /// </summary>
    /// <param name="existingDocuments">List of documents that already exist in the system.</param>
    /// <returns>A collection of new or modified PDF documents to be ingested.</returns>
    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = SourceFileVersion(sourceFile);
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentVersion != sourceFileVersion)
            {
                results.Add(new() { Key = Guid.CreateVersion7(), SourceId = SourceId, DocumentId = sourceFileId, DocumentVersion = sourceFileVersion });
            }
        }

        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    /// <summary>
    /// Gets PDF documents that have been deleted from the directory since the last ingestion.
    /// </summary>
    /// <param name="existingDocuments">List of documents that currently exist in the system.</param>
    /// <returns>A collection of documents that should be removed from the system.</returns>
    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        var currentFileIds = currentFiles.ToLookup(SourceFileId);
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    /// <summary>
    /// Creates text chunks from the specified PDF document for vector storage.
    /// </summary>
    /// <param name="document">The PDF document to process into chunks.</param>
    /// <returns>A collection of text chunks extracted from the PDF document.</returns>
    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        using var pdf = PdfDocument.Open(Path.Combine(sourceDirectory, document.DocumentId));
        var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();

        return Task.FromResult(paragraphs.Select(p => new IngestedChunk
        {
            Key = Guid.CreateVersion7(),
            DocumentId = document.DocumentId,
            PageNumber = p.PageNumber,
            Text = p.Text,
        }));
    }

    /// <summary>
    /// Extracts paragraphs from a PDF page using document layout analysis.
    /// </summary>
    /// <param name="pdfPage">The PDF page to extract paragraphs from.</param>
    /// <returns>A collection of paragraphs with their page number, index, and text content.</returns>
    private static IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}