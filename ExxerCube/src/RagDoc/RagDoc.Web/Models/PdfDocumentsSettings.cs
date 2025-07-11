namespace RagDoc.Web.Models
{
    /// <summary>
    /// Configuration settings for PDF document processing.
    /// </summary>
    public class PdfDocumentsSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether PDF document processing is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the collection of directories to scan for PDF documents.
        /// </summary>
        public IEnumerable<string> Directories { get; set; }
    }
}
