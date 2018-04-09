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
    /// these tests overwrite the AAD auth file(s) and environment variables so run these tests only when required
    /// </summary>
    [Fact, IsDev]
    public void AADFlow_NoCredentials()
    {
        ClearAADCredentials();
        EswDevOpsSdk.CreateAADContext().Should().BeNull();
    }

    /// <summary>
    /// these tests overwrite the AAD auth file(s) and environment variables so run these tests only when required
    /// </summary>
    [Fact, IsDev]
    public void AADFlow_AuthFile()
    {
        ClearAADCredentials();

        var appLocal = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expectedPath = Path.Combine(appLocal, "dummy.azureauth");
        File.WriteAllText(expectedPath, "dummy");
        var context = EswDevOpsSdk.CreateAADContext();
        context.AuthFilePath.Should().Be(expectedPath);
        context.TenantId.Should().BeNull();
        context.ClientId.Should().BeNull();
        context.ClientSecret.Should().BeNull();
        context.SubscriptionId.Should().BeNull();

        ClearAADCredentials();        
    }

    /// <summary>
    /// these tests overwrite the AAD auth file(s) and environment variables so run these tests only when required
    /// </summary>
    [Theory, IsDev]
    [InlineData(EnvironmentVariableTarget.Process)]
    [InlineData(EnvironmentVariableTarget.User)]
    [InlineData(EnvironmentVariableTarget.Machine)]
    public void AADFlow_EnvVariables_AllLevels(EnvironmentVariableTarget target)
    {
        ClearAADCredentials();        
        SetEnvVariableContext(target:target);

        var context = EswDevOpsSdk.CreateAADContext();

        context.ClientId.Should().Be("clientId");
        context.ClientSecret.Should().Be("clientSecret");

        ClearAADCredentials();
    }

    /// <summary>
    /// these tests overwrite the AAD auth file(s) and environment variables so run these tests only when required
    /// </summary>
    [Fact, IsDev]
    public void AADFlow_EnvVariables_CheckTenantId()
    {
        ClearAADCredentials();
        SetEnvVariableContext();

        var context = EswDevOpsSdk.CreateAADContext();
        context.TenantId.Should().Be("3e14278f-8366-4dfd-bcc8-7e4e9d57f2c1");

        ClearAADCredentials();
    }

    /// <summary>
    /// these tests overwrite the AAD auth file(s) and environment variables so run these tests only when required
    /// </summary>
    [Theory, IsDev]
    [InlineData("CI", "30c09ef3-7f8a-4a13-a864-776438027e9d")]
    [InlineData("TEST", "49c77085-e8c5-4ad2-8114-1d4e71a64cc1")]
    public void AADFlow_EnvVariables_CheckSubscriptionMapping(string envName, string expected)
    {
        ClearAADCredentials();
        SetEnvVariableContext(envName: envName);

        var context = EswDevOpsSdk.CreateAADContext();

        context.SubscriptionId.Should().Be(expected);

        ClearAADCredentials();
    }

    private static void SetEnvVariableContext(string clientId = "clientId",
        string clientSecret = "clientSecret", EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, string envName = "CI")
    {
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, envName, target);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientIdEnvVariable, clientId, target);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientSecretEnvVariable, clientSecret, target);
    }

    private static void ClearAADCredentials()
    {        
        //clear temp folder
        var appLocalFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        foreach (var file in Directory.GetFiles(appLocalFolder, "*.azureauth"))
        {
            File.Delete(file);
        }

        //clear env variables
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientIdEnvVariable, null,  EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientSecretEnvVariable, null, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientIdEnvVariable, null, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientSecretEnvVariable, null, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientIdEnvVariable, null, EnvironmentVariableTarget.Machine);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.AADClientSecretEnvVariable, null, EnvironmentVariableTarget.Machine);
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
