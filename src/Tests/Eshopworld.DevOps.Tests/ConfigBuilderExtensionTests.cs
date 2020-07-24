using System;
using System.Collections.Generic;
using System.IO;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Xunit;

public class ConfigurationExtentionsTests : IDisposable
{
    private const string TestAppSettingsFile = "appsettings_unit.json";
    private const string TestKubernetestSecretFile = "TestKey2";
    private readonly string _kubernetesTestSecretPath;

    public ConfigurationExtentionsTests()
    {
        // Setup test files.
        var currentDir = Directory.GetCurrentDirectory();
        _kubernetesTestSecretPath = currentDir;

        // Method 1 - app settings json file.
        File.WriteAllText(Path.Combine(currentDir, TestAppSettingsFile), "{\"TestKey1\":\"testVal1\", \"TestKey2\": { \"TestKey3\":\"testVal3\" } }");

        // Method 2 - Kubernetes Secrets simulation.
        File.WriteAllText(Path.Combine(currentDir, TestKubernetestSecretFile), "testVal2");
    }

    /// <summary>Ensure BindBaseSection on the IConfigurationBuilder, binds root appsettings to a model as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_BindBaseSection()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();

        // Act
        configBuilder.UseDefaultConfigs(TestAppSettingsFile, _kubernetesTestSecretPath);
        var boundConfig = configBuilder.Build().BindBaseSection<TestSettings>();

        // Assert
        boundConfig.TestKey1.Should().Be("testVal1");
        boundConfig.TestKey2.Should().Be("testVal2");
    }

    /// <summary>Ensure AddKubernetesSecrets on the IConfigurationBuilder, adds config from Kubernetes secrets as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_AddKubernetesSecrets()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("testKey", "testVal")
        });

        // Act
        configBuilder.AddKubernetesSecrets(_kubernetesTestSecretPath);
        var lookupResult = configBuilder.GetValue<string>("TestKey2");

        // Assert
        lookupResult.Should().NotBeNullOrEmpty();
        lookupResult.Should().Be("testVal2");
    }

    /// <summary>Ensure using default configs loads all expected values from various sources (in the expected order).</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_UseDefaultConfigs()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder()
            .UseDefaultConfigs(TestAppSettingsFile, Directory.GetCurrentDirectory())
            .Build();

        var settings = configBuilder.Get<TestSettings>();

        // Act/Assert
        settings.Should().NotBeNull();
        settings.TestKey1.Should().NotBeNullOrEmpty(); // loaded from appsettings
        settings.TestKey1.Should().Be("testVal1");
        settings.TestKey2.Should().NotBeNullOrEmpty(); // loaded from kubernetes secret
        settings.TestKey2.Should().Be("testVal2");
    }

    /// <summary>Ensure Use Default Builder with wrong paths still sets up the config with the right path.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_UseDefaultConfigsWrongPaths()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder()
            .UseDefaultConfigs("madeUpSettings.json", _kubernetesTestSecretPath)
            .Build();

        // Act
        var settings = configBuilder.Get<TestSettings>();

        // Assert
        settings.Should().NotBeNull();
        settings.TestKey1.Should().BeNullOrEmpty(); // loaded from appsettings
        settings.TestKey2.Should().NotBeNullOrEmpty(); // loaded from kubernetes secret
        settings.TestKey2.Should().Be("testVal2");
    }

    /// <summary>Ensure add value, adds a config setting as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_AddValue()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddValue("testKey", "testVal");

        // Act
        var lookupResult = configBuilder.GetValue<string>("testKey");

        // Assert
        lookupResult.Should().Be("testVal");
    }

    /// <summary>Ensure add multiple values, adds as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_AddValues()
    {
        // Arrange
        var configs = new List<KeyValuePair<string, string>> {
            new KeyValuePair<string, string>("testKey", "testVal"),
            new KeyValuePair<string, string>("testKey1", "testVal1"),
            new KeyValuePair<string, string>("testKey2", "testVal2"),
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddValues(configs);

        // Act
        var lookupResult = configBuilder.GetValue<string>("testKey");
        var lookupResult1 = configBuilder.GetValue<string>("testKey1");
        var lookupResult2 = configBuilder.GetValue<string>("testKey2");

        // Assert
        lookupResult.Should().Be("testVal");
        lookupResult1.Should().Be("testVal1");
        lookupResult2.Should().Be("testVal2");
    }

    /// <summary>Ensure GetValue on the builder, returns the value as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_GetValue()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("testKey", "testVal")
        });

        // Act
        var lookupResult = configBuilder.GetValue<string>("testKey");

        // Assert
        lookupResult.Should().Be("testVal");
    }

    /// <summary>Ensure wrong keys don't return values.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_TryGetValue_FailWithWrongKey()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder()
            .UseDefaultConfigs(TestAppSettingsFile, Directory.GetCurrentDirectory())
            .Build();

        // Act/Assert
        configBuilder.TryGetValue("WrongKey", out string value).Should().BeFalse();
        value.Should().BeNullOrEmpty();
    }

    /// <summary>Ensure trying to get a value using a null key returns false for TryGet.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_TryGetValue_FailWithNullKey()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder()
            .UseDefaultConfigs(TestAppSettingsFile, Directory.GetCurrentDirectory())
            .Build();

        // Act/Assert
        configBuilder.TryGetValue(null, out string value).Should().BeFalse();
        value.Should().BeNullOrEmpty();
    }

    /// <summary>Ensure TryGet returns expected value and successful flag.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_TryGetValue()
    {
        IConfiguration configBuilder = new ConfigurationBuilder()
            .UseDefaultConfigs(TestAppSettingsFile, Directory.GetCurrentDirectory())
            .Build();

        // Act/Assert
        configBuilder.TryGetValue("TestKey1", out string value).Should().BeTrue();
        value.Should().NotBeNullOrEmpty();
    }

    /// <summary>Verify an argument exception occurs when "KEYVAULT_URL" and "KeyVaultInstanceName" is not set.</summary>
    [Fact, IsUnit]
    public void Test_KeyVault_Builder_AddKeyVaultSecretsWithParams_NoUrl()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        Action loadSettings = () => { builder.AddKeyVaultSecrets("key1", "key2"); };

        // Assert
        loadSettings.Should().Throw<ArgumentException>();
    }

    /// <summary>Verify invalid operation exception when wrong key vault is set.</summary>
    [Fact, IsUnit]
    public void Test_KeyVault_Builder_AddKeyVaultSecretsWithParams_InvalidKeyVault()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        builder.AddValue("KeyVaultInstanceName", "WrongKv");
        Action loadSettings = () => { builder.AddKeyVaultSecrets("key1", "key2"); };

        // Assert
        loadSettings.Should().Throw<InvalidOperationException>();
    }

    /// <summary>Verify argument exception occurs when trying to add secrets but vault is not specified.</summary>
    [Fact, IsUnit]
    public void Test_KeyVault_Builder_AddKeyVaultSecrets_NoUri()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        Action loadSettings = () => { builder.AddKeyVaultSecrets(null, new List<string> {"key1", "key2"}); };

        // Assert
        loadSettings.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verify key is not set but no error occurs when trying to add a key in key vault that does not exist.
    /// PRESUMPTION: KEYVAULT_URL is set on the test server.
    /// </summary>
    [Fact, IsIntegration]
    public void Test_KeyVault_Builder_AddKeyVaultSecrets_SecretNotAdded()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        builder.UseDefaultConfigs();
        builder.AddKeyVaultSecrets("MadeUpKey1");
        var config = builder.Build();

        // Assert
        config.TryGetValue<object>("MadeUpKey1", out _).Should().BeFalse();
    }

    /// <summary>
    /// Verify a real key in keyvault is added as expected.
    /// PRESUMPTION: KEYVAULT_URL is set on the test server.
    /// </summary>
    [Fact, IsIntegration]
    public void Test_KeyVault_Builder_AddKeyVaultSecrets_SecretAdded()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        builder.UseDefaultConfigs();

        // Set the test value in KV.
        var vaultUrl = builder.GetValue<string>("KEYVAULT_URL");
        var vault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
        vault.SetSecretAsync(vaultUrl, "RealKey1", "MyValue1").GetAwaiter().GetResult();

        // Add the secret to the builder using extension method.
        builder.AddKeyVaultSecrets("RealKey1");
        var config = builder.Build();

        // Assert
        config.TryGetValue<object>("MadeUpKey1", out _).Should().BeFalse();
    }

    public void Dispose()
    {
        // Clean up test files.
        var currentDir = Directory.GetCurrentDirectory();
        File.Delete(Path.Combine(currentDir, TestAppSettingsFile));
        File.Delete(Path.Combine(currentDir, TestKubernetestSecretFile));
    }

    private class TestSettings
    {
        public string TestKey1 { get; set; }
        public string TestKey2 { get; set; }
    }
}
