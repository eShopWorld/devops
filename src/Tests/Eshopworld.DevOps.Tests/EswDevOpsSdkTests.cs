using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once InconsistentNaming
// ReSharper disable once CheckNamespace
public class EswDevOpsSdkTests
{
    [Fact, IsDev]
    public void BuildConfiguration_ReadFromCoreAppSettings()
    {
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, "ENV1"); //process level is fine here
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["KeyRootAppSettings"].Should().BeEquivalentTo("AppSettingsValue");
    }


    [Fact, IsDev]
    public void BuildConfiguration_NonTestMode()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["KeyTestAppSettings"].Should().BeNullOrEmpty();
    }


    [Fact, IsDev]
    public void BuildConfiguration_ReadFromEnvironmentalAppSettings()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "ENV1");

        sut["KeyENV1AppSettings"].Should().BeEquivalentTo("ENV1AppSettingsValue");
    }


    [Fact, IsDev]
    public void BuildConfiguration_ReadFromEnvironmentalVariable()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["PATH"].Should().NotBeNullOrEmpty();
    }

    [Fact, IsDev]
    public void BuildConfiguration_TestMode()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, useTest: true);
        sut["KeyTestAppSettings"].Should().Be("IntegrationAppSettingsValue");
    }

    [Fact, IsLayer1]
    public void BuildConfiguration_MSIAuthenticationTest()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "CI", true);
        sut["keyVaultItem"].Should().Be("keyVaultItemValue");
    }

    private const string SierraIntegration = "si";

    [Theory, IsDev]
    [InlineData(EnvironmentNames.PROD, EnvironmentNames.TEST, EnvironmentNames.TEST)]
    [InlineData(EnvironmentNames.PROD, EnvironmentNames.CI, EnvironmentNames.CI)]
    [InlineData(EnvironmentNames.PROD, EnvironmentNames.SAND, EnvironmentNames.SAND)]
    [InlineData(EnvironmentNames.PROD, EnvironmentNames.PREP, EnvironmentNames.PREP)]
    [InlineData(EnvironmentNames.PROD, EnvironmentNames.PROD, EnvironmentNames.PROD)]
    [InlineData(EnvironmentNames.CI, EnvironmentNames.PROD, SierraIntegration)]
    [InlineData(EnvironmentNames.CI, EnvironmentNames.SAND, SierraIntegration)]
    [InlineData(EnvironmentNames.CI, EnvironmentNames.CI, SierraIntegration)]
    [InlineData(EnvironmentNames.PREP, EnvironmentNames.CI, SierraIntegration)]
    [InlineData(EnvironmentNames.SAND, EnvironmentNames.PROD, SierraIntegration)]
    [InlineData(EnvironmentNames.SAND, EnvironmentNames.SAND, SierraIntegration)]
    public void GetDeploymentSubscriptionIdTest(string environmentName, string deploymentEnvironmentName, string resultEnvironmentSubscription)
    {
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName);
        var expectedSubscriptionId = resultEnvironmentSubscription == SierraIntegration
            ? EswDevOpsSdk.SierraIntegrationSubscriptionId
            : EswDevOpsSdk.GetSubscriptionId(deploymentEnvironmentName);

        var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(deploymentEnvironmentName);

        subscriptionId.Should().Be(expectedSubscriptionId);
    }

    [Fact, IsDev]
    public void GetSubscriptionId_works_for_known_environments()
    {
        var environmentNames = typeof(EnvironmentNames).GetFields().Select(x => (string)x.GetValue(null));
        foreach (var environmentName in environmentNames)
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName);
            var subscriptionId = EswDevOpsSdk.GetSubscriptionId();
            subscriptionId.Should().NotBeNullOrEmpty();
        }
    }

    [Theory, IsDev]
    [InlineData(EnvironmentNames.CI, Regions.WestEurope, new[] {Regions.WestEurope})]
    [InlineData(EnvironmentNames.PROD, Regions.WestEurope, new[] { Regions.WestEurope, Regions.EastUS })]
    [InlineData(EnvironmentNames.PROD, Regions.EastUS, new[] { Regions.EastUS, Regions.WestEurope })]
    public void GetRegionSequence_ForAllEnvironments(string env, Regions source, Regions[] expected)
    {
        var ret = EswDevOpsSdk.GetRegionSequence(env, source);
        ret.Should().ContainInOrder(expected);
    }
    public class CreateDeploymentContext
    {
        [Theory, IsDev]
        [InlineData("West Europe", new[] { "West Europe", "East US" })]
        [InlineData("East US", new[] { "East US", "West Europe" })]
        public void ForAllProductionRegions(string regionValue, string[] expectedRegionHierarchy)
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.Machine);

            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, regionValue, EnvironmentVariableTarget.Machine);

            EswDevOpsSdk.CreateDeploymentContext().PreferredRegions.Should().ContainInOrder(expectedRegionHierarchy);
        }

        [Fact, IsDev]
        public void ForCIReturnWEOnly()
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, null, EnvironmentVariableTarget.Machine);

            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, "West Europe", EnvironmentVariableTarget.Machine);

            EswDevOpsSdk.CreateDeploymentContext("CI").PreferredRegions.Should().ContainInOrder("West Europe");
        }
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
