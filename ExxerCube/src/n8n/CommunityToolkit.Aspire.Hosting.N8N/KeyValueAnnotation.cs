using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.N8N
{
    /// <summary>
    /// Represents a key-value annotation for use with Aspire resource builders.
    /// Implements <see cref="IResourceAnnotation"/> to allow attaching arbitrary metadata to resources.
    /// </summary>
    public class KeyValueAnnotation : IResourceAnnotation
    {
        /// <summary>
        /// Gets the annotation key.
        /// </summary>
        public string Key { get; }
        /// <summary>
        /// Gets the annotation value.
        /// </summary>
        public string Value { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueAnnotation"/> class with the specified key and value.
        /// </summary>
        /// <param name="key">The annotation key.</param>
        /// <param name="value">The annotation value.</param>
        public KeyValueAnnotation(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
