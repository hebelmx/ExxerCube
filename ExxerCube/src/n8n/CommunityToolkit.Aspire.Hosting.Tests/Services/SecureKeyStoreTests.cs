using ExxerAI.Orchestration.Services;
using ExxerAI.Orchestration.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace ExxerAI.Orchestration.Tests.Services;

/// <summary>
/// Comprehensive unit tests for SecureKeyStore implementation
/// Tests encryption, storage, retrieval, and security features
/// </summary>
public class SecureKeyStoreTests : IDisposable
{
    private readonly ILogger<SecureKeyStore> _mockLogger;
    private readonly string _testStorePath;
    private readonly SecureKeyStore _keyStore;

    public SecureKeyStoreTests()
    {
        _mockLogger = Substitute.For<ILogger<SecureKeyStore>>();
        _testStorePath = Path.Combine(Path.GetTempPath(), $"test_keystore_{Guid.NewGuid()}.json");
        _keyStore = new SecureKeyStore(_mockLogger, _testStorePath, "test-encryption-key");
    }

    public void Dispose()
    {
        if (File.Exists(_testStorePath))
        {
            File.Delete(_testStorePath);
        }
    }

    [Fact]
    public async Task SetKeyAsync_WithValidData_ShouldStoreSuccessfullyAsync()
    {
        // Arrange
        var keyName = "test-api-key";
        var value = "sk-1234567890abcdef";
        var scope = "openai";

        // Act
        await _keyStore.SetKeyAsync(keyName, value, scope, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var retrievedValue = await _keyStore.GetKeyAsync(keyName, scope, cancellationToken: TestContext.Current.CancellationToken);
        retrievedValue.ShouldBe(value);
    }

    [Fact]
    public async Task GetKeyAsync_WithNonExistentKey_ShouldReturnNullAsync()
    {
        // Arrange
        var keyName = "non-existent-key";

        // Act
        var result = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetKeyAsync_WithScopeFilter_ShouldReturnCorrectKeyAsync()
    {
        // Arrange
        var keyName = "api-key";
        var value1 = "value-for-scope1";
        var value2 = "value-for-scope2";
        var scope1 = "openai";
        var scope2 = "anthropic";

        // Act
        await _keyStore.SetKeyAsync(keyName, value1, scope1, cancellationToken: TestContext.Current.CancellationToken);
        await _keyStore.SetKeyAsync(keyName, value2, scope2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var result1 = await _keyStore.GetKeyAsync(keyName, scope1, cancellationToken: TestContext.Current.CancellationToken);
        var result2 = await _keyStore.GetKeyAsync(keyName, scope2, cancellationToken: TestContext.Current.CancellationToken);

        result1.ShouldBe(value1);
        result2.ShouldBe(value2);
    }

    [Fact]
    public async Task DeleteKeyAsync_WithExistingKey_ShouldReturnTrueAndRemoveKeyAsync()
    {
        // Arrange
        var keyName = "deletable-key";
        var value = "test-value";
        await _keyStore.SetKeyAsync(keyName, value, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var deleted = await _keyStore.DeleteKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);
        var retrievedAfterDelete = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        deleted.ShouldBeTrue();
        retrievedAfterDelete.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteKeyAsync_WithNonExistentKey_ShouldReturnFalseAsync()
    {
        // Arrange
        var keyName = "non-existent-key";

        // Act
        var deleted = await _keyStore.DeleteKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        deleted.ShouldBeFalse();
    }

    [Fact]
    public async Task KeyExistsAsync_WithExistingKey_ShouldReturnTrueAsync()
    {
        // Arrange
        var keyName = "existing-key";
        var value = "test-value";
        await _keyStore.SetKeyAsync(keyName, value, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exists = await _keyStore.KeyExistsAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task KeyExistsAsync_WithNonExistentKey_ShouldReturnFalseAsync()
    {
        // Arrange
        var keyName = "non-existent-key";

        // Act
        var exists = await _keyStore.KeyExistsAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ListKeysAsync_WithMultipleKeys_ShouldReturnAllKeysAsync()
    {
        // Arrange
        var keys = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        foreach (var kvp in keys)
        {
            await _keyStore.SetKeyAsync(kvp.Key, kvp.Value, cancellationToken: TestContext.Current.CancellationToken);
        }

        // Act
        var listedKeys = await _keyStore.ListKeysAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        listedKeys.ShouldContain("key1");
        listedKeys.ShouldContain("key2");
        listedKeys.ShouldContain("key3");
        listedKeys.Count().ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ListKeysAsync_WithScopeFilter_ShouldReturnOnlyKeysInScopeAsync()
    {
        // Arrange
        var scope1 = "scope1";
        var scope2 = "scope2";

        await _keyStore.SetKeyAsync("key1", "value1", scope1, cancellationToken: TestContext.Current.CancellationToken);
        await _keyStore.SetKeyAsync("key2", "value2", scope1, cancellationToken: TestContext.Current.CancellationToken);
        await _keyStore.SetKeyAsync("key3", "value3", scope2, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var scope1Keys = await _keyStore.ListKeysAsync(scope1, cancellationToken: TestContext.Current.CancellationToken);
        var scope2Keys = await _keyStore.ListKeysAsync(scope2, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        scope1Keys.Count().ShouldBe(2);
        scope2Keys.Count().ShouldBe(1);
        scope1Keys.ShouldContain($"{scope1}:key1");
        scope1Keys.ShouldContain($"{scope1}:key2");
        scope2Keys.ShouldContain($"{scope2}:key3");
    }

    [Fact]
    public async Task RotateKeyAsync_WithExistingKey_ShouldCreateBackupAndUpdateKeyAsync()
    {
        // Arrange
        var keyName = "rotatable-key";
        var originalValue = "original-value";
        var newValue = "new-value";
        await _keyStore.SetKeyAsync(keyName, originalValue, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _keyStore.RotateKeyAsync(keyName, newValue, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var currentValue = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);
        currentValue.ShouldBe(newValue);

        // Verify backup was created
        var allKeys = await _keyStore.ListKeysAsync(cancellationToken: TestContext.Current.CancellationToken);
        allKeys.ShouldContain(k => k.Contains("_backup_"));
    }

    [Fact]
    public async Task GenerateApiKeyAsync_ShouldCreateRandomKeyWithCorrectLengthAsync()
    {
        // Arrange
        var keyName = "generated-key";
        var length = 48;

        // Act
        var generatedKey = await _keyStore.GenerateApiKeyAsync(keyName, null, length, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        generatedKey.ShouldNotBeNullOrEmpty();
        generatedKey.Length.ShouldBe(length);

        // Verify it was stored
        var storedKey = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);
        storedKey.ShouldBe(generatedKey);
    }

    [Fact]
    public async Task SetKeyAsync_WithExpiration_ShouldExpireAfterTimeoutAsync()
    {
        // Arrange
        var keyName = "expiring-key";
        var value = "temporary-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _keyStore.SetKeyAsync(keyName, value, null, expiration, cancellationToken: TestContext.Current.CancellationToken);

        // Verify key exists initially
        var initialValue = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);
        initialValue.ShouldBe(value);

        // Wait for expiration
        await Task.Delay(expiration.Add(TimeSpan.FromMilliseconds(50)), cancellationToken: TestContext.Current.CancellationToken);

        // Assert key has expired
        var expiredValue = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);
        expiredValue.ShouldBeNull();
    }

    [Theory]
    [InlineData("simple-key", "simple-value")]
    [InlineData("special@key#name", "value-with-special!@#characters")]
    [InlineData("unicode-key-🔑", "unicode-value-🔐")]
    [InlineData("very-long-key-name-that-exceeds-normal-length", "very-long-value-with-lots-of-content-that-tests-encryption-with-larger-data")]
    public async Task EncryptionDecryption_WithVariousInputs_ShouldMaintainDataIntegrityAsync(string keyName, string value)
    {
        // Act
        await _keyStore.SetKeyAsync(keyName, value, cancellationToken: TestContext.Current.CancellationToken);
        var retrievedValue = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        retrievedValue.ShouldBe(value);
    }

    [Fact]
    public async Task GetKeyAsync_WithEnvironmentVariableSet_ShouldPreferEnvironmentValueAsync()
    {
        // Arrange
        var keyName = "env-test-key";
        var storedValue = "stored-value";
        var envValue = "environment-value";
        var envVarName = $"LOCALAI_{keyName.ToUpperInvariant().Replace("-", "_")}";

        // Set environment variable
        Environment.SetEnvironmentVariable(envVarName, envValue);

        try
        {
            // Store value in key store
            await _keyStore.SetKeyAsync(keyName, storedValue, cancellationToken: TestContext.Current.CancellationToken);

            // Act
            var retrievedValue = await _keyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

            // Assert - should return environment value
            retrievedValue.ShouldBe(envValue);
        }
        finally
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable(envVarName, null);
        }
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldBeSafeAndConsistentAsync()
    {
        // Arrange
        var tasks = new List<Task>();
        var keyPrefix = "concurrent-key-";
        var expectedValues = new Dictionary<string, string>();

        // Create concurrent operations
        for (int i = 0; i < 10; i++)
        {
            var keyName = $"{keyPrefix}{i}";
            var value = $"value-{i}";
            expectedValues[keyName] = value;

            tasks.Add(_keyStore.SetKeyAsync(keyName, value, cancellationToken: TestContext.Current.CancellationToken));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert - verify all keys were stored correctly
        foreach (var kvp in expectedValues)
        {
            var retrievedValue = await _keyStore.GetKeyAsync(kvp.Key, cancellationToken: TestContext.Current.CancellationToken);
            retrievedValue.ShouldBe(kvp.Value);
        }
    }

    [Fact]
    public async Task PersistenceTest_ShouldMaintainDataAcrossInstancesAsync()
    {
        // Arrange
        var keyName = "persistence-test";
        var value = "persistent-value";

        // Store in first instance
        await _keyStore.SetKeyAsync(keyName, value, cancellationToken: TestContext.Current.CancellationToken);

        // Create new instance with same store path
        var newKeyStore = new SecureKeyStore(_mockLogger, _testStorePath, "test-encryption-key");

        // Wait a moment for initialization
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var retrievedValue = await newKeyStore.GetKeyAsync(keyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        retrievedValue.ShouldBe(value);
    }
}