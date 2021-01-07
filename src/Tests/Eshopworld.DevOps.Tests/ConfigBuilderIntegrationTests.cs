using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

public class ConfigBuilderIntegrationTests
{
    /// <summary>Ensure using default configs loads all expected values from various sources (in the expected order).</summary>
    [Fact, IsIntegration]
    public void Test_ConfigBuilder_UseDefaultConfigs()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder()
            .AddValue("TestKey1", "Override this setting!") // this will be overriden by the value loaded in appsettings
            .UseDefaultConfigs()
            .Build();

        var settings = configBuilder.Get<TestSettings>();

        // Act/Assert
        settings.Should().NotBeNull();
        settings.TestKey1.Should().NotBeNullOrEmpty(); // loaded from appsettings
        settings.TestKey1.Should().Be("testVal1");
    }

    /// <summary>Ensure using default configs loads all expected values from various sources (in the expected order).</summary>
    [Fact, IsIntegration]
    public void Test_ConfigBuilder_UseDefaultConfigs_for_multiple_files()
    {
        // Arrange
        var configFiles = new[]
        {
            "appsettings.json",
            "appSettings.ORDER.json",
            "appsettings.TEST.json"
        };

        // Act
        IConfiguration configuration = new ConfigurationBuilder()
            .UseDefaultConfigs(configFiles)
            .Build();

        // Assert
        configuration.TryGetValue<string>("Option1", out var result).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().Be("option1-from-test");
    }

    [Theory, IsIntegration]
    [InlineData("CI", null, "West Europe", null, "CI", "CI-WE-secret")]
    [InlineData("CI", null, "West Europe", "WE", "CI", "CI-WE-secret")]
    [InlineData("CI", "CI", "West Europe", null, "CI", "CI-WE-secret")]
    [InlineData("CI", "CI", "West Europe", "WE", "CI", "CI-WE-secret")]
    [InlineData("CI", null, "East US", null, "CI", "CI-EUS-secret")]
    [InlineData("CI", null, "East US", "EUS", "CI", "CI-EUS-secret")]
    [InlineData("CI", "CI", "East US", null, "CI", "CI-EUS-secret")]
    [InlineData("CI", "CI", "East US", "EUS", "CI", "CI-EUS-secret")]
    [InlineData("CI", null, "Southeast Asia", null, "CI", "CI-SA-secret")]
    [InlineData("CI", null, "Southeast Asia", "SA", "CI", "CI-SA-secret")]
    [InlineData("CI", "CI", "Southeast Asia", null, "CI", "CI-SA-secret")]
    [InlineData("CI", "CI", "Southeast Asia", "SA", "CI", "CI-SA-secret")]
    [InlineData("TEST", null, "West Europe", null, "default", "default-secret")]
    [InlineData("TEST", null, "West Europe", "WE", "default", "default-secret")]
    [InlineData("TEST", "TEST", "West Europe", null, "default", "default-secret")]
    [InlineData("TEST", "TEST", "West Europe", "WE", "default", "default-secret")]
    public void Test_ConfigBuilder_UseDefaultConfigs_For_Regional_Config
        (string envValue, string envParam, string regionValue, string regionParam, string expectedKey, string expectedSecret)
    {
        var prevEnv = Environment.GetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, envValue, EnvironmentVariableTarget.Process);

        var prevRegion = Environment.GetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, regionValue, EnvironmentVariableTarget.Process);
        
        try
        {
            // Arrange
            
            // Act
            IConfiguration configuration = new ConfigurationBuilder()
                .UseDefaultConfigs(environment: envParam, deploymentRegion: regionParam)
                .Build();

            // Assert
            configuration.TryGetValue<string>("RegionalConfig:Key", out var key).Should().BeTrue();
            key.Should().NotBeNull();
            key.Should().Be(expectedKey);

            configuration.TryGetValue<string>("RegionalConfig:Secret", out var secret).Should().BeTrue();
            secret.Should().NotBeNull();
            secret.Should().Be(expectedSecret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, prevEnv, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, prevRegion, EnvironmentVariableTarget.Process);
        }
    }

    /// <summary>Verify invalid operation exception when wrong key vault is set.</summary>
    [Fact, IsIntegration]
    public void Test_KeyVault_Builder_AddKeyVaultSecretsWithParams_InvalidKeyVault()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        builder.AddValue(EswDevOpsSdk.KeyVaultUrlKey, "WrongKv");
        Action loadSettings = () => { builder.AddKeyVaultSecrets("key1", "key2"); };

        // Assert
        loadSettings.Should().Throw<InvalidOperationException>();
    }

    /// <summary>Verify argument exception occurs when trying to add secrets but vault is not specified.</summary>
    [Fact, IsIntegration]
    public void Test_KeyVault_Builder_AddKeyVaultSecrets_NoUri()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        Action loadSettings = () => { builder.AddKeyVaultSecrets(null, new[] { "key1", "key2" }); };

        // Assert
        loadSettings.Should().Throw<ArgumentNullException>();
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
        builder.AddKeyVaultSecrets("keyVaultItem");
        var config = builder.Build();

        // Assert
        config.TryGetValue<object>("keyVaultItem", out var result).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().Be("keyVaultItemValue");
    }

    /// <summary>
    /// Verify a real key in keyvault is added as expected and that it can be mapped to a config name that is different.
    /// </summary>
    [Fact, IsIntegration]
    public void Test_KeyVault_Builder_AddKeyVaultSecrets_MapConfigName()
    {
        // Arrange - Principle needs "Set" permissions to run this.
        IConfigurationBuilder builder = new ConfigurationBuilder();

        // Act
        builder.UseDefaultConfigs();
        builder.AddKeyVaultSecrets(new Dictionary<string, string>
        {
            { "keyVaultItem", "MappedName"}
        });
        var config = builder.Build();

        // Assert
        config.TryGetValue<object>("MappedName", out var result).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().Be("keyVaultItemValue");
    }

    private class TestSettings
    {
        public string TestKey1 { get; set; }
    }
}
