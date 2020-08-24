using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Eshopworld.DevOps.KeyVault;
using Xunit;

public class ConfigBuilderUnitTests
{
    /// <summary>Ensure BindBaseSection on the IConfigurationBuilder, binds root appsettings to a model as expected.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_BindBaseSection()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();

        // Act
        configBuilder.AddValue("TestKey1", "testVal1")
                     .AddValue("TestKey2", "testVal2")
                     .AddValue("Test-Key-3", "testVal3")
                     .AddValue("Test-Key-4", "testVal4")
                     .AddValue("SubProp--SubKey1", "testVal1")
                     .AddValue("Sub-Prop--Sub-Key2", "testVal2");
        var boundConfig = configBuilder.Build().BindBaseSection<TestSettings>();

        // Assert
        boundConfig.TestKey1.Should().Be("testVal1");
        boundConfig.TestKey2.Should().Be("testVal2");
        boundConfig.TestKey3.Should().Be("testVal3");
        boundConfig.TestKey4.Should().Be("testVal4");
        boundConfig.SubProp.Should().NotBeNull();
        boundConfig.SubProp.SubKey1.Should().Be("testVal1");
        boundConfig.SubProp.SubKey2.Should().Be("testVal2");
    }

    /// <summary>Ensure BindBaseSection on the IConfigurationBuilder, binds root appsettings to a model as expected.</summary>
    [Fact, IsUnit]
    public void Test_Config_BindSection_WithNoKVMapping()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder();
        configBuilder
            .AddValue("KeyVaultInstanceName", "kvinstance")
            .AddValue("Section:TestKey1", "testVal1")
            .AddValue("Section:TestKey2", "testVal2")
            .AddValue("Section:TestKey3", "testVal3")
            .AddValue("Section:TestKey4", "testVal4");

        // Act
        var boundConfig = configBuilder.Build().BindSection<TestSettings>("Section");

        // Assert
        boundConfig.TestKey1.Should().Be("testVal1");
        boundConfig.TestKey2.Should().Be("testVal2");
        boundConfig.TestKey3.Should().Be("testVal3");
        boundConfig.TestKey4.Should().Be("testVal4");
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
        var configs = new Dictionary<string, string> {
            { "testKey", "testVal" },
            { "testKey1", "testVal1" },
            { "testKey2", "testVal2" }
        };
        var configBuilder = new ConfigurationBuilder();

        // Act
        configBuilder.AddValues(configs);
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
        IConfiguration configBuilder = new ConfigurationBuilder().AddValue("TestKey1", "testVal1").Build();

        // Act/Assert
        configBuilder.TryGetValue("WrongKey", out string value).Should().BeFalse();
        value.Should().BeNullOrEmpty();
    }

    /// <summary>Ensure trying to get a value using a null key returns false for TryGet.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_TryGetValue_FailWithNullKey()
    {
        // Arrange
        IConfiguration configBuilder = new ConfigurationBuilder().AddValue("TestKey1", "testVal1").Build();

        // Act/Assert
        configBuilder.TryGetValue(null, out string value).Should().BeFalse();
        value.Should().BeNullOrEmpty();
    }

    /// <summary>Ensure TryGet returns expected value and successful flag.</summary>
    [Fact, IsUnit]
    public void Test_ConfigBuilder_TryGetValue()
    {
        IConfiguration configBuilder = new ConfigurationBuilder().AddValue("TestKey1", "testVal1").Build();

        // Act/Assert
        configBuilder.TryGetValue("TestKey1", out string value).Should().BeTrue();
        value.Should().NotBeNullOrEmpty();
    }

    private class TestKvSettings
    {
        public string TestKey1 { get; set; }
        public string TestKey2 { get; set; }
        [KeyVaultSecretName("kv-name-3")]
        public string TestKey3 { get; set; }
        [KeyVaultSecretName("kv-name-4")]
        public string TestKey4 { get; set; }
    }

    private class SubClass
    {
        public string SubKey1 { get; set; }
        public string SubKey2 { get; set; }
    }

    private class TestSettings
    {
        public string TestKey1 { get; set; }
        public string TestKey2 { get; set; }
        public string TestKey3 { get; set; }
        public string TestKey4 { get; set; }
        public SubClass SubProp { get; set; }
    }
}
