using System;
using System.IO;
using System.Reflection;
using Eshopworld.DevOps;
using Eshopworld.DevOps.Tests;
using Eshopworld.Tests.Core;
using Xunit;
using FluentAssertions;

// ReSharper disable once InconsistentNaming
// ReSharper disable once CheckNamespace
public class EswDevOpsSdkTests : IClassFixture<TestsFixture>
{
    [Fact, IsIntegration]
    public void Test_ReadFromCoreAppSettings()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["KeyRootAppSettings"].Should().BeEquivalentTo("AppSettingsValue");
    }


    [Fact, IsIntegration]
    public void Test_ReadFromTestAppSettings()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, useTest: true);

        sut["KeyTestAppSettings"].Should().BeEquivalentTo("TestAppSettingsValue");
    }



    [Fact, IsIntegration]
    public void Test_ReadFromEnvironmentalAppSettings()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "ENV1");

        sut["KeyENV1AppSettings"].Should().BeEquivalentTo("ENV1AppSettingsValue");
    }


    [Fact, IsIntegration]
    public void Test_ReadFromEnvironmentalVariable()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["PATH"].Should().NotBeNullOrEmpty();
    }


    [Fact, IsIntegration]
    public void Test_ReadFromKeyVault()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["keyVaultItem"].Should().BeEquivalentTo("keyVaultItemValue");
    }



    /// <summary>
    /// the test runner will shadow copy the assemblies so to resolve the config files this is needed
    /// </summary>
    public static string AssemblyDirectory
    {
        get
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
