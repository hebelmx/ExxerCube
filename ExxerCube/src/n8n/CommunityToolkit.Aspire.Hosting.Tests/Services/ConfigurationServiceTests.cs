using ExxerAI.Orchestration.Services;
using ExxerAI.Orchestration.Configuration;
using ExxerAI.Orchestration.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ExxerAI.Orchestration.Tests.Services;

/// <summary>
/// Comprehensive unit tests for ConfigurationService
/// Tests configuration loading, environment overrides, and key management integration
/// </summary>
public class ConfigurationServiceTests : IDisposable
{
    private readonly ILogger<ConfigurationService> _mockLogger;
    private readonly LocalAIKeyManager _keyManager;
    private readonly string _tempStorePath;

    public ConfigurationServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<ConfigurationService>>();

        // Create a real key manager for testing with a temporary store
        _tempStorePath = Path.Combine(Path.GetTempPath(), $"config_test_{Guid.NewGuid()}.json");
        var mockKeyStoreLogger = Substitute.For<ILogger<SecureKeyStore>>();
        var mockKeyManagerLogger = Substitute.For<ILogger<LocalAIKeyManager>>();
        var keyStore = new SecureKeyStore(mockKeyStoreLogger, _tempStorePath, "test-key");
        _keyManager = new LocalAIKeyManager(keyStore, mockKeyManagerLogger);
    }

    public void Dispose()
    {
        if (File.Exists(_tempStorePath))
        {
            File.Delete(_tempStorePath);
        }
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldInitializeCorrectly()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Assert
        service.ShouldNotBeNull();
        service.Configuration.ShouldNotBeNull();
        service.Configuration.LocalAI.ShouldNotBeNull();
        service.Configuration.Database.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithoutKeyManager_ShouldApplyEnvironmentOverrides()
    {
        // Arrange
        var testPassword = "test-db-password";
        var testUsername = "test-db-user";
        var testApiKey = "test-api-key";

        Environment.SetEnvironmentVariable("LOCALAI_DB_PASSWORD", testPassword);
        Environment.SetEnvironmentVariable("LOCALAI_DB_USERNAME", testUsername);
        Environment.SetEnvironmentVariable("LOCALAI_API_KEY", testApiKey);

        try
        {
            var config = CreateTestConfiguration();

            // Act
            var service = new ConfigurationService(config, null!, _mockLogger);

            // Assert
            service.Configuration.Database.Password.ShouldBe(testPassword);
            service.Configuration.Database.Username.ShouldBe(testUsername);
            service.Configuration.LocalAI.ApiKey.ShouldBe(testApiKey);
        }
        finally
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("LOCALAI_DB_PASSWORD", null);
            Environment.SetEnvironmentVariable("LOCALAI_DB_USERNAME", null);
            Environment.SetEnvironmentVariable("LOCALAI_API_KEY", null);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithKeyManager_ShouldInitializeSecurelyAsync()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await _keyManager.Received(1).InitializeKeysAsync(Arg.Any<LocalAIStackConfiguration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_WithoutKeyManager_ShouldLogWarningAsync()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, null!, _mockLogger);

        // Act
        await service.InitializeAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockLogger.Received().LogWarning(Arg.Any<string>());
    }

    [Fact]
    public void GetServiceUrls_ShouldReturnAllExpectedServices()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act
        var serviceUrls = service.GetServiceUrls();

        // Assert
        serviceUrls.ShouldNotBeNull();
        serviceUrls.ShouldContainKey("Aspire Dashboard");
        serviceUrls.ShouldContainKey("Health Dashboard");
        serviceUrls.ShouldContainKey("LocalAI API");
        serviceUrls.ShouldContainKey("Open WebUI");
        serviceUrls.ShouldContainKey("SearXNG");
        serviceUrls.ShouldContainKey("Supabase REST");
        serviceUrls.ShouldContainKey("Supabase Auth");
        serviceUrls.ShouldContainKey("Qdrant");
        serviceUrls.ShouldContainKey("Milvus");
        serviceUrls.ShouldContainKey("Prometheus");
        serviceUrls.ShouldContainKey("Grafana");
        serviceUrls.ShouldContainKey("Nginx Gateway");
        serviceUrls.ShouldContainKey("Redis");

        // Verify URL formats
        serviceUrls["LocalAI API"].ShouldContain("localhost:8080");
        serviceUrls["Qdrant"].ShouldContain("localhost:6333");
        serviceUrls["Redis"].ShouldContain("localhost:6379");
    }

    [Fact]
    public async Task GetSecureDatabaseConnectionStringAsync_WithKeyManager_ShouldUseSecureMethodAsync()
    {
        // Arrange
        var expectedConnectionString = "Host=localhost;Port=5432;Database=test;Username=secure_user;Password=secure_pass";
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        _keyManager.GetDatabaseConnectionStringAsync(Arg.Any<DatabaseConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedConnectionString));

        // Act
        var connectionString = await service.GetSecureDatabaseConnectionStringAsync(TestContext.Current.CancellationToken);

        // Assert
        connectionString.ShouldBe(expectedConnectionString);
        await _keyManager.Received(1).GetDatabaseConnectionStringAsync(Arg.Any<DatabaseConfiguration>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSecureDatabaseConnectionStringAsync_WithoutKeyManager_ShouldUseFallbackAsync()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, null!, _mockLogger);

        // Act
        var connectionString = await service.GetSecureDatabaseConnectionStringAsync(TestContext.Current.CancellationToken);

        // Assert
        connectionString.ShouldNotBeNullOrEmpty();
        connectionString.ShouldContain("Host=localhost");
        connectionString.ShouldContain("Port=5432");
        connectionString.ShouldContain("Database=exxerai");
    }

    [Fact]
    public async Task GetSecureLocalAIApiKeyAsync_WithKeyManager_ShouldUseSecureMethodAsync()
    {
        // Arrange
        var expectedApiKey = "secure-api-key-123";
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        _keyManager.GetLocalAIApiKeyAsync(TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<string?>(expectedApiKey));

        // Act
        var apiKey = await service.GetSecureLocalAIApiKeyAsync(TestContext.Current.CancellationToken);

        // Assert
        apiKey.ShouldBe(expectedApiKey);
        await _keyManager.Received(1).GetLocalAIApiKeyAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSecureLocalAIApiKeyAsync_WithoutKeyManager_ShouldUseFallbackAsync()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, null!, _mockLogger);

        // Act
        var apiKey = await service.GetSecureLocalAIApiKeyAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        apiKey.ShouldBe("default-api-key");
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("huggingface")]
    public async Task GetExternalApiKeyAsync_WithKeyManager_ShouldUseSecureMethodAsync(string provider)
    {
        // Arrange
        var expectedApiKey = $"secure-{provider}-key";
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        _keyManager.GetExternalApiKeyAsync(provider, TestContext.Current.CancellationToken)
            .Returns(Task.FromResult<string?>(expectedApiKey));

        // Act
        var apiKey = await service.GetExternalApiKeyAsync(provider, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        apiKey.ShouldBe(expectedApiKey);
        await _keyManager.Received(1).GetExternalApiKeyAsync(provider, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetExternalApiKeyAsync_WithoutKeyManager_ShouldCheckEnvironmentVariableAsync()
    {
        // Arrange
        var provider = "openai";
        var expectedApiKey = "env-openai-key";
        var envVarName = "LOCALAI_EXTERNAL_OPENAI_API_KEY";

        Environment.SetEnvironmentVariable(envVarName, expectedApiKey);

        try
        {
            var config = CreateTestConfiguration();
            var service = new ConfigurationService(config, null!, _mockLogger);

            // Act
            var apiKey = await service.GetExternalApiKeyAsync(provider, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            apiKey.ShouldBe(expectedApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public async Task SetExternalApiKeyAsync_WithKeyManager_ShouldStoreSecurelyAsync()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "new-openai-key";
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act
        await service.SetExternalApiKeyAsync(provider, apiKey, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await _keyManager.Received(1).SetExternalApiKeyAsync(provider, apiKey, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SetExternalApiKeyAsync_WithoutKeyManager_ShouldLogWarningAsync()
    {
        // Arrange
        var provider = "openai";
        var apiKey = "new-openai-key";
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, null!, _mockLogger);

        // Act
        await service.SetExternalApiKeyAsync(provider, apiKey, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockLogger.Received().LogWarning(Arg.Any<string>());
    }

    [Fact]
    public void GetDatabaseConnectionString_ShouldReturnFormattedConnectionString()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act
        var connectionString = service.GetDatabaseConnectionString();

        // Assert
        connectionString.ShouldNotBeNullOrEmpty();
        connectionString.ShouldContain("Host=localhost");
        connectionString.ShouldContain("Port=5432");
        connectionString.ShouldContain("Database=exxerai");
        connectionString.ShouldContain("Username=exxerai_user");
        connectionString.ShouldContain("Password=default-password");
    }

    [Fact]
    public void GetLocalAIApiUrl_ShouldReturnCorrectUrl()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act
        var apiUrl = service.GetLocalAIApiUrl();

        // Assert
        apiUrl.ShouldBe("http://localhost:8080/v1");
    }

    [Fact]
    public void DisplayServiceUrls_ShouldNotThrow()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var service = new ConfigurationService(config, _keyManager, _mockLogger);

        // Act & Assert
        Should.NotThrow(() => service.DisplayServiceUrls());
    }

    [Fact]
    public void ApplyEnvironmentOverrides_WithAllVariablesSet_ShouldOverrideAllValues()
    {
        // Arrange
        var testValues = new Dictionary<string, string>
        {
            ["LOCALAI_DB_PASSWORD"] = "env-password",
            ["LOCALAI_DB_USERNAME"] = "env-username",
            ["SUPABASE_JWT_SECRET"] = "env-jwt-secret",
            ["SUPABASE_ANON_KEY"] = "env-anon-key",
            ["LOCALAI_API_KEY"] = "env-api-key",
            ["QDRANT_API_KEY"] = "env-qdrant-key",
            ["LOCALAI_ENCRYPTION_KEY"] = "env-encryption-key"
        };

        // Set environment variables
        foreach (var kvp in testValues)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        try
        {
            var config = CreateTestConfiguration();

            // Act
            var service = new ConfigurationService(config, null!, _mockLogger);

            // Assert
            service.Configuration.Database.Password.ShouldBe(testValues["LOCALAI_DB_PASSWORD"]);
            service.Configuration.Database.Username.ShouldBe(testValues["LOCALAI_DB_USERNAME"]);
            service.Configuration.Database.Supabase.JwtSecret.ShouldBe(testValues["SUPABASE_JWT_SECRET"]);
            service.Configuration.Database.Supabase.AnonKey.ShouldBe(testValues["SUPABASE_ANON_KEY"]);
            service.Configuration.LocalAI.ApiKey.ShouldBe(testValues["LOCALAI_API_KEY"]);
            service.Configuration.VectorDatabases.Qdrant.ApiKey.ShouldBe(testValues["QDRANT_API_KEY"]);
            service.Configuration.Security.EncryptionKey.ShouldBe(testValues["LOCALAI_ENCRYPTION_KEY"]);
        }
        finally
        {
            // Clean up environment variables
            foreach (var kvp in testValues)
            {
                Environment.SetEnvironmentVariable(kvp.Key, null);
            }
        }
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
            ["LocalAIStack:Database:Supabase:JwtSecret"] = "default-jwt-secret",
            ["LocalAIStack:Database:Supabase:AnonKey"] = "default-anon-key",
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
            ["LocalAIStack:Security:EncryptionKey"] = "default-encryption-key"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}