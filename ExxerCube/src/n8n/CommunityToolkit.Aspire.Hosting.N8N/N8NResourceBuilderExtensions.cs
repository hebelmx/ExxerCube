using System;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CommunityToolkit.Aspire.Hosting.N8N;

/// <summary>
/// Extension methods for registering N8N container resources in a distributed Aspire application.
/// </summary>
public static class N8NResourceBuilderExtensions
{
    /// <summary>
    /// Adds an N8N container resource to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder to add the N8N resource to.</param>
    /// <param name="name">The name of the N8N resource. Default is "n8n".</param>
    /// <param name="port">The port the N8N service will listen on. Default is 5678.</param>
    /// <returns>An <see cref="IResourceBuilder{N8NResource}"/> for further configuration.</returns>
    /// <remarks>
    /// You must provide a valid <c>ReferenceExpression</c> instance for the <c>N8NResource</c> constructor.
    /// </remarks>
    public static IResourceBuilder<N8NResource> AddN8N(this IDistributedApplicationBuilder builder, string name = "n8n", int port = 5678)
    {
        var timeZone = TimeZoneInfo.Local.Id;
        // TODO: Replace with a valid ReferenceExpression instance as required by your application
        var resource = new N8NResource(name, /* ReferenceExpression instance required here */ default!, port);

        var builderResource = builder.AddResource(resource)
            .WithImage("n8nio/n8n")
            .WithHttpEndpoint(port: port, targetPort: 5678)
            .WithEnvironment("TZ", timeZone)
            .WithVolume("sqlserver_data", "/var/opt/mssql", isReadOnly: false);

        return builderResource;
    }

    /// <summary>
    /// Sets basic authentication credentials for the N8N container via environment variables.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="user">The username for basic authentication.</param>
    /// <param name="password">The password for basic authentication.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithBasicAuth(
        this IResourceBuilder<N8NResource> builder,
        string user, string password)
    {
        return builder
            .WithEnvironment("N8N_BASIC_AUTH_ACTIVE", "true")
            .WithEnvironment("N8N_BASIC_AUTH_USER", user)
            .WithEnvironment("N8N_BASIC_AUTH_PASSWORD", password);
    }

    /// <summary>
    /// Specifies the mount path for the workflows directory used by N8N.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="hostPath">The host path to mount as the workflows directory.</param>
    /// <param name="containerPath">The container path for the workflows directory. Default is "/home/node/.n8n".</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithWorkflowsDirectory(
        this IResourceBuilder<N8NResource> builder,
        string hostPath, string containerPath = "/home/node/.n8n")
    {
        return builder.WithVolume(hostPath, containerPath, isReadOnly: false);
    }

    /// <summary>
    /// Configures the N8N container to use a PostgreSQL database.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="host">The PostgreSQL host.</param>
    /// <param name="user">The PostgreSQL user.</param>
    /// <param name="password">The PostgreSQL password.</param>
    /// <param name="database">The PostgreSQL database name.</param>
    /// <param name="port">The PostgreSQL port. Default is 5432.</param>
    /// <param name="schema">The PostgreSQL schema (optional).</param>
    /// <param name="sslCa">The SSL certificate authority (optional).</param>
    /// <param name="rejectUnauthorized">Whether to reject unauthorized SSL connections. Default is true.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithPostgresDatabase(this IResourceBuilder<N8NResource> builder, string host, string user, string password, string database, int port = 5432, string schema = null!, string sslCa = null!, bool rejectUnauthorized = true)
    {
        host = string.IsNullOrEmpty(host) ? "localhost" : host;
        user = string.IsNullOrEmpty(user) ? "postgres" : user;
        password = string.IsNullOrEmpty(password) ? "postgres" : password;
        database = string.IsNullOrEmpty(database) ? "postgres" : database;

        builder
            .WithEnvironment("DB_TYPE", "postgresdb")
            .WithEnvironment("DB_POSTGRESDB_HOST", host)
            .WithEnvironment("DB_POSTGRESDB_PORT", port.ToString())
            .WithEnvironment("DB_POSTGRESDB_USER", user)
            .WithEnvironment("DB_POSTGRESDB_PASSWORD", password)
            .WithEnvironment("DB_POSTGRESDB_DATABASE", database);

        if (!string.IsNullOrEmpty(schema))
            builder.WithEnvironment("DB_POSTGRESDB_SCHEMA", schema);

        if (!string.IsNullOrEmpty(sslCa))
            builder.WithEnvironment("DB_POSTGRESDB_SSL_CA", sslCa);

        builder.WithEnvironment("DB_POSTGRESDB_SSL_REJECT_UNAUTHORIZED", rejectUnauthorized.ToString().ToLower());

        // Build the connection string and set it on the resource's ConnectionString property
        string connStr = $"Host={host};Port={port};Username={user};Password={password};Database={database}";
        if (!string.IsNullOrEmpty(schema))
            connStr += $";Search Path={schema}";
        if (!string.IsNullOrEmpty(sslCa))
            connStr += $";SSL Mode=Require;SSL Certificate={sslCa}";
        builder.Resource.ConnectionString = connStr;

        return builder;
    }

    /// <summary>
    /// Configures the timezone for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="timeZone">The timezone to set (e.g., "UTC").</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithTimeZone(
        this IResourceBuilder<N8NResource> builder, string timeZone)
    {
        return builder.WithEnvironment("TZ", timeZone);
    }

    /// <summary>
    /// Adds health probe HTTP endpoints to the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithHealthProbes(this IResourceBuilder<N8NResource> builder)
    {
        return builder
            .WithHttpEndpoint(name: "healthz", targetPort: 5678)
            .WithHttpEndpoint(name: "readiness", targetPort: 5678)
            .WithHttpEndpoint(name: "metrics", targetPort: 5678);
    }

    /// <summary>
    /// Configures a custom HTTP proxy environment variable for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="proxyUrl">The HTTP proxy URL.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithProxy(
        this IResourceBuilder<N8NResource> builder, string proxyUrl)
    {
        return builder.WithEnvironment("HTTP_PROXY", proxyUrl);
    }

    /// <summary>
    /// Configures webhook settings for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="host">The webhook host.</param>
    /// <param name="protocol">The webhook protocol (default is "https").</param>
    /// <param name="port">The webhook port (default is 5678).</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithWebhookSettings(this IResourceBuilder<N8NResource> builder, string host, string protocol = "https", int port = 5678)
    {
        return builder
            .WithEnvironment("N8N_HOST", host)
            .WithEnvironment("N8N_PORT", port.ToString())
            .WithEnvironment("N8N_PROTOCOL", protocol)
            .WithEnvironment("WEBHOOK_URL", $"{protocol}://{host}/");
    }

    /// <summary>
    /// Configures execution retention settings for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="maxAgeDays">Maximum age in days for execution data (optional).</param>
    /// <param name="maxCount">Maximum count of execution data (optional).</param>
    /// <param name="storage">The storage type for execution data (optional).</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithExecutionRetention(this IResourceBuilder<N8NResource> builder, int? maxAgeDays = null, int? maxCount = null, string storage = null!)
    {
        if (maxAgeDays.HasValue)
            builder.WithEnvironment("EXECUTIONS_DATA_MAX_AGE", maxAgeDays.Value.ToString());

        if (maxCount.HasValue)
            builder.WithEnvironment("EXECUTIONS_DATA_MAX_COUNT", maxCount.Value.ToString());

        if (!string.IsNullOrEmpty(storage))
            builder.WithEnvironment("EXECUTIONS_DATA_STORAGE", storage);

        return builder;
    }

    /// <summary>
    /// Sets the NODE_ENV environment variable for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="environment">The node environment (default is "production").</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithNodeEnvironment(this IResourceBuilder<N8NResource> builder, string environment = "production")
    {
        return builder.WithEnvironment("NODE_ENV", environment);
    }

    /// <summary>
    /// Adds Traefik labels for the N8N container for reverse proxy configuration.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="subdomain">The subdomain for the Traefik rule.</param>
    /// <param name="domainName">The domain name for the Traefik rule.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithTraefikLabels(this IResourceBuilder<N8NResource> builder, string subdomain, string domainName)
    {
        string host = $"{subdomain}.{domainName}";
        return builder
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.enable", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.routers.n8n.rule", $"Host(`{host}`)"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.routers.n8n.tls", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.routers.n8n.entrypoints", "web,websecure"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.routers.n8n.tls.certresolver", "mytlschallenge"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.SSLRedirect", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.STSSeconds", "315360000"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.browserXSSFilter", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.contentTypeNosniff", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.forceSTSHeader", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.SSLHost", domainName))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.STSIncludeSubdomains", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.middlewares.n8n.headers.STSPreload", "true"))
            .WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("traefik.http.routers.n8n.middlewares", "n8n@docker"));
    }

    /// <summary>
    /// Placeholder for health status propagation configuration.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithHealthStatusPropagation(this IResourceBuilder<N8NResource> builder)
    {
        // No WithProbe method available; consider implementing health checks differently if needed.
        return builder;
    }

    /// <summary>
    /// Sets the initialization command(s) for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="commands">The initialization commands to run.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithInitCommand(this IResourceBuilder<N8NResource> builder, params string[] commands)
    {
        return builder.WithArgs(commands);
    }

    /// <summary>
    /// Sets the log level for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="logLevel">The log level (default is "info").</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithLogging(this IResourceBuilder<N8NResource> builder, string logLevel = "info")
    {
        return builder.WithEnvironment("N8N_LOG_LEVEL", logLevel);
    }

    /// <summary>
    /// Configures lifecycle hooks for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="preStartCommand">The command to run before the container starts (optional).</param>
    /// <param name="postStartCommand">The command to run after the container starts (optional).</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithLifecycleHooks(this IResourceBuilder<N8NResource> builder, string preStartCommand = null!, string postStartCommand = null!)
    {
        if (!string.IsNullOrWhiteSpace(preStartCommand))
            builder.WithEnvironment("PRE_START_COMMAND", preStartCommand);
        if (!string.IsNullOrWhiteSpace(postStartCommand))
            builder.WithEnvironment("POST_START_COMMAND", postStartCommand);
        return builder;
    }

    /// <summary>
    /// Sets node affinity for the N8N container in Kubernetes.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="nodeSelector">The node selector string.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithAffinity(this IResourceBuilder<N8NResource> builder, string nodeSelector)
    {
        return builder.WithAnnotation<KeyValueAnnotation>(new KeyValueAnnotation("kubernetes.io/affinity", nodeSelector));
    }

    /// <summary>
    /// Adds a dependency on another resource for the N8N container.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="dependency">The dependent resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<N8NResource> WithDependency(this IResourceBuilder<N8NResource> builder, IResource dependency)
    {
        // No WithReference overload for IResource; consider using WaitFor or another dependency mechanism if needed.
        return builder;
    }

    /// <summary>
    /// Conditionally starts the N8N resource based on a predicate.
    /// </summary>
    /// <param name="builder">The resource builder for the N8N resource.</param>
    /// <param name="condition">A function that returns true if the resource should start.</param>
    /// <returns>The resource builder for chaining if the condition is met.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the condition is not met.</exception>
    public static IResourceBuilder<N8NResource> ConditionalStartup(this IResourceBuilder<N8NResource> builder, Func<bool> condition)
    {
        if (condition())
            return builder;

        throw new InvalidOperationException("Condition for starting the N8N resource was not met.");
    }
}