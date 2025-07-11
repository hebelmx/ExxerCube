using ExxerAI.Orchestration.Services;
using ExxerAI.Orchestration.Configuration;
using ExxerAI.Orchestration.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ExxerAI.Orchestration.Tests;

/// <summary>
/// Integration tests for ExxerAI Orchestration system components
/// Tests the interaction between configuration, key management, and service orchestration
/// </summary>
public class OrchestrationIntegrationTests
{
    private readonly ILogger<ConfigurationService> _mockConfigLogger;
    private readonly ILogger<SecureKeyStore> _mockKeyStoreLogger;
    private readonly ILogger<LocalAIKeyManager> _mockKeyManagerLogger;

    public OrchestrationIntegrationTests()
    {
        _mockConfigLogger = Substitute.For<ILogger<ConfigurationService>>();
        _mockKeyStoreLogger = Substitute.For<ILogger<SecureKeyStore>>();
        _mockKeyManagerLogger = Substitute.For<ILogger<LocalAIKeyManager>>();
    }

    [Fact]
    public void OrchestrationComponents_ShouldInitializeCorrectly()
    {
        // Arrange
        var configuration = CreateTestConfiguration();

        // Act
        var configService = new ConfigurationService(configuration, null!, _mockConfigLogger);

        // Assert
        configService.ShouldNotBeNull();
        configService.Configuration.ShouldNotBeNull();
        configService.Configuration.LocalAI.ApiPort.ShouldBe(8080);
    }

    [Fact]
    public async Task FullOrchestrationStack_WithSecureKeyStore_ShouldWorkTogetherAsync()
    {
        // Arrange
        var tempStorePath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.json");
        var keyStore = new SecureKeyStore(_mockKeyStoreLogger, tempStorePath, "integration-test-key");
        var keyManager = new LocalAIKeyManager(keyStore, _mockKeyManagerLogger);
        var configuration = CreateTestConfiguration();
        var configService = new ConfigurationService(configuration, keyManager, _mockConfigLogger);

        try
        {
            // Act
            await configService.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Store some test keys
            await keyStore.SetKeyAsync("test-api-key", "test-value", "integration-test", cancellationToken: TestContext.Current.CancellationToken);
            await keyStore.SetKeyAsync("openai-key", "sk-test123", "external", cancellationToken: TestContext.Current.CancellationToken);

            // Test retrieval
            var retrievedKey = await keyStore.GetKeyAsync("test-api-key", "integration-test", cancellationToken: TestContext.Current.CancellationToken);
            var externalKey = await configService.GetExternalApiKeyAsync("openai", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            retrievedKey.ShouldBe("test-value");
            externalKey.ShouldNotBeNull();

            // Test service URLs
            var serviceUrls = configService.GetServiceUrls();
            serviceUrls.ShouldContainKey("LocalAI API");
            serviceUrls["LocalAI API"].ShouldBe("http://localhost:8080/v1");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempStorePath))
            {
                File.Delete(tempStorePath);
            }
        }
    }

    [Fact]
    public async Task ConfigurationService_WithEnvironmentOverrides_ShouldPrioritizeEnvironmentVariablesAsync()
    {
        // Arrange
        var testApiKey = "env-override-api-key";
        var testDbPassword = "env-override-db-password";

        Environment.SetEnvironmentVariable("LOCALAI_API_KEY", testApiKey);
        Environment.SetEnvironmentVariable("LOCALAI_DB_PASSWORD", testDbPassword);

        try
        {
            var configuration = CreateTestConfiguration();
            var configService = new ConfigurationService(configuration, null!, _mockConfigLogger);

            // Act
            await configService.InitializeAsync(TestContext.Current.CancellationToken);
            var apiKey = await configService.GetSecureLocalAIApiKeyAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            apiKey.ShouldBe(testApiKey);
            configService.Configuration.Database.Password.ShouldBe(testDbPassword);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOCALAI_API_KEY", null);
            Environment.SetEnvironmentVariable("LOCALAI_DB_PASSWORD", null);
        }
    }

    [Fact]
    public async Task KeyManager_DatabaseConnectionString_ShouldConstructCorrectlyAsync()
    {
        // Arrange
        var tempStorePath = Path.Combine(Path.GetTempPath(), $"db_test_{Guid.NewGuid()}.json");
        var keyStore = new SecureKeyStore(_mockKeyStoreLogger, tempStorePath, "db-test-key");
        var keyManager = new LocalAIKeyManager(keyStore, _mockKeyManagerLogger);

        var dbConfig = new DatabaseConfiguration
        {
            Host = "test-host",
            Port = 5432,
            DatabaseName = "test-db",
            Username = "test-user",
            Password = "test-password"
        };

        try
        {
            // Act
            var connectionString = await keyManager.GetDatabaseConnectionStringAsync(dbConfig, TestContext.Current.CancellationToken);

            // Assert
            connectionString.ShouldNotBeNullOrEmpty();
            connectionString.ShouldContain("Host=test-host");
            connectionString.ShouldContain("Port=5432");
            connectionString.ShouldContain("Database=test-db");
            connectionString.ShouldContain("Username=test-user");
            connectionString.ShouldContain("Password=test-password");
        }
        finally
        {
            if (File.Exists(tempStorePath))
            {
                File.Delete(tempStorePath);
            }
        }
    }

    [Fact]
    public async Task EndToEndConfiguration_AllServices_ShouldProvideValidEndpointsAsync()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var configService = new ConfigurationService(configuration, null!, _mockConfigLogger);

        // Act
        await configService.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);
        var serviceUrls = configService.GetServiceUrls();
        var dbConnectionString = configService.GetDatabaseConnectionString();
        var localAIUrl = configService.GetLocalAIApiUrl();

        // Assert
        serviceUrls.ShouldNotBeEmpty();
        serviceUrls.Count.ShouldBeGreaterThan(10);

        // Verify critical service URLs
        serviceUrls.ShouldContainKey("LocalAI API");
        serviceUrls.ShouldContainKey("Qdrant");
        serviceUrls.ShouldContainKey("Redis");
        serviceUrls.ShouldContainKey("Supabase REST");

        dbConnectionString.ShouldNotBeNullOrEmpty();
        localAIUrl.ShouldBe("http://localhost:8080/v1");

        // Verify all URLs are well-formed
        foreach (var kvp in serviceUrls)
        {
            if (Uri.TryCreate(kvp.Value, UriKind.Absolute, out var uri))
            {
                uri.Host.ShouldBe("localhost");
                uri.Port.ShouldBeGreaterThan(0);
            }
            else
            {
                // Redis connection string is different format
                kvp.Key.ShouldBe("Redis");
                kvp.Value!.ShouldStartWith("redis://");
            }
        }
    }

    [Fact]
    public async Task ConcurrentKeyOperations_AcrossMultipleServices_ShouldBeSafeAsync()
    {
        // Arrange
        var tempStorePath = Path.Combine(Path.GetTempPath(), $"concurrent_test_{Guid.NewGuid()}.json");
        var keyStore = new SecureKeyStore(_mockKeyStoreLogger, tempStorePath, "concurrent-test-key");
        var keyManager = new LocalAIKeyManager(keyStore, _mockKeyManagerLogger);
        var configuration = CreateTestConfiguration();
        var configService = new ConfigurationService(configuration, keyManager, _mockConfigLogger);

        try
        {
            // Act - Simulate concurrent operations from multiple services
            var tasks = new List<Task>
            {
                // Configuration service operations
                configService.InitializeAsync(TestContext.Current.CancellationToken),
                configService.SetExternalApiKeyAsync("openai", "openai-key-123", TestContext.Current.CancellationToken),
                configService.SetExternalApiKeyAsync("anthropic", "anthropic-key-456", TestContext.Current.CancellationToken),

                // Direct key store operations
                keyStore.SetKeyAsync("localai-key", "localai-value", "ai",
               cancellationToken: TestContext.Current.CancellationToken),
                keyStore.SetKeyAsync("vector-db-key", "vector-value", "database"
                , cancellationToken: TestContext.Current.CancellationToken),
                keyStore.SetKeyAsync("monitoring-key", "monitoring-value", "ops"
                , cancellationToken: TestContext.Current.CancellationToken)
            };

            // Wait for all operations to complete
            await Task.WhenAll(tasks);

            // Verify all keys were stored correctly
            var openaiKey = await configService.GetExternalApiKeyAsync("openai", cancellationToken: TestContext.Current.CancellationToken);
            var anthropicKey = await configService.GetExternalApiKeyAsync("anthropic", cancellationToken: TestContext.Current.CancellationToken);
            var localaiKey = await keyStore.GetKeyAsync("localai-key", "ai", TestContext.Current.CancellationToken);
            var vectorKey = await keyStore.GetKeyAsync("vector-db-key", "database", cancellationToken: TestContext.Current.CancellationToken);
            var monitoringKey = await keyStore.GetKeyAsync("monitoring-key", "ops", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            openaiKey.ShouldBe("openai-key-123");
            anthropicKey.ShouldBe("anthropic-key-456");
            localaiKey.ShouldBe("localai-value");
            vectorKey.ShouldBe("vector-value");
            monitoringKey.ShouldBe("monitoring-value");
        }
        finally
        {
            if (File.Exists(tempStorePath))
            {
                File.Delete(tempStorePath);
            }
        }
    }

    [Fact]
    public void OrchestrationArchitecture_ShouldFollowDependencyInjectionPattern()
    {
        // Arrange & Act
        var configuration = CreateTestConfiguration();

        // Test that all main components can be instantiated with proper dependency injection
        var keyStore = new SecureKeyStore(_mockKeyStoreLogger);
        var keyManager = new LocalAIKeyManager(keyStore, _mockKeyManagerLogger);
        var configService = new ConfigurationService(configuration, keyManager, _mockConfigLogger);

        // Assert
        keyStore.ShouldNotBeNull();
        keyManager.ShouldNotBeNull();
        configService.ShouldNotBeNull();

        // Verify the dependency chain works
        configService.Configuration.ShouldNotBeNull();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["LocalAIStack:Database:Host"] = "localhost",
            ["LocalAIStack:Database:Port"] = "5432",
            ["LocalAIStack:Database:DatabaseName"] = "exxerai",
            ["LocalAIStack:Database:Username"] = "exxerai_user",
            ["LocalAIStack:Database:Password"] = "default-password",
            ["LocalAIStack:Database:Supabase:JwtSecret"] = "test-jwt-secret",
            ["LocalAIStack:Database:Supabase:AnonKey"] = "test-anon-key",
            ["LocalAIStack:Database:Supabase:RestPort"] = "54321",
            ["LocalAIStack:Database:Supabase:AuthPort"] = "54322",
            ["LocalAIStack:LocalAI:ApiKey"] = "default-api-key",
            ["LocalAIStack:LocalAI:ApiPort"] = "8080",
            ["LocalAIStack:LocalAI:WebUIPort"] = "8081",
            ["LocalAIStack:VectorDatabases:Qdrant:Port"] = "6333",
            ["LocalAIStack:VectorDatabases:Qdrant:ApiKey"] = "default-qdrant-key",
            ["LocalAIStack:VectorDatabases:Milvus:WebPort"] = "9091",
            ["LocalAIStack:Search:Port"] = "8888",
            ["LocalAIStack:Monitoring:Prometheus:Port"] = "9090",
            ["LocalAIStack:Monitoring:Grafana:Port"] = "3000",
            ["LocalAIStack:Network:Nginx:HttpPort"] = "80",
            ["LocalAIStack:Network:Redis:Port"] = "6379",
            ["LocalAIStack:Security:EncryptionKey"] = "test-encryption-key"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}