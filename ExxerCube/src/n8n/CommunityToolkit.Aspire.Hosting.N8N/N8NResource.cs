using System;
using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.N8N
{
    /// <summary>
    /// Represents the N8N container resource for Aspire, with connection string expression support and additional configuration.
    /// </summary>
    public sealed class N8NResource : ContainerResource, IResourceWithConnectionString
    {
        private ReferenceExpression _connectionStringExpression;

        /// <summary>
        /// Gets or sets the plain connection string for the N8N resource.
        /// </summary>
        public string ConnectionString { get; set; } = null!;

        /// <summary>
        /// Gets or sets the connection string expression for the N8N resource.
        /// </summary>
        public ReferenceExpression ConnectionStringExpression
        {
            get => _connectionStringExpression;
            set => _connectionStringExpression = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="N8NResource"/> class with the specified name, port, and connection string expression.
        /// </summary>
        /// <param name="name">The name of the N8N resource.</param>
        /// <param name="connectionStringExpression">The connection string expression for the N8N resource.</param>
        /// <param name="port">The port the N8N service will listen on. Default is 5678.</param>
        public N8NResource(string name, ReferenceExpression connectionStringExpression, int port = 5678) : base(name)
        {
            Port = port;
            _connectionStringExpression = connectionStringExpression ?? throw new ArgumentNullException(nameof(connectionStringExpression));
        }

        /// <summary>
        /// Gets the port the N8N service will listen on.
        /// </summary>
        public int Port { get; }
    }
}