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
    [InlineData("CI", DeploymentEnvironment.CI)]
    [InlineData("PREP", DeploymentEnvironment.Prep)]
    [InlineData("pRep", DeploymentEnvironment.Prep)]
    public void GeEnvironmentTest(string envValue, DeploymentEnvironment env)
    {
        var prevEnv = Environment.GetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, envValue);
        try
        {
            var currentEnvironment = EswDevOpsSdk.GetEnvironment();
            currentEnvironment.Should().Be(env);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, prevEnv);
        }
    }

    [Theory, IsDev]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("PR")]
    public void GeEnvironmentFailsTest(string env)
    {
        var prevEnv = Environment.GetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, env);
        try
        {
            Action func = () => EswDevOpsSdk.GetEnvironment();
            func.Should().Throw<DevOpsSDKException>();
        }
        finally
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, prevEnv);
        }
    }

    [Theory, IsDev]
    [InlineData(DeploymentEnvironment.Prod, DeploymentEnvironment.Test, DeploymentEnvironment.Test)]
    [InlineData(DeploymentEnvironment.Prod, DeploymentEnvironment.CI, DeploymentEnvironment.CI)]
    [InlineData(DeploymentEnvironment.Prod, DeploymentEnvironment.Sand, DeploymentEnvironment.Sand)]
    [InlineData(DeploymentEnvironment.Prod, DeploymentEnvironment.Prep, DeploymentEnvironment.Prep)]
    [InlineData(DeploymentEnvironment.Prod, DeploymentEnvironment.Prod, DeploymentEnvironment.Prod)]
    [InlineData(DeploymentEnvironment.CI, DeploymentEnvironment.Prod, SierraIntegration)]
    [InlineData(DeploymentEnvironment.CI, DeploymentEnvironment.Sand, SierraIntegration)]
    [InlineData(DeploymentEnvironment.CI, DeploymentEnvironment.CI, SierraIntegration)]
    [InlineData(DeploymentEnvironment.Prep, DeploymentEnvironment.CI, SierraIntegration)]
    [InlineData(DeploymentEnvironment.Sand, DeploymentEnvironment.Prod, SierraIntegration)]
    [InlineData(DeploymentEnvironment.Sand, DeploymentEnvironment.Sand, SierraIntegration)]
    public void GetDeploymentSubscriptionIdTest(DeploymentEnvironment environmentName, DeploymentEnvironment deploymentEnvironmentName, object resultEnvironmentSubscription)
    {
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName.ToString());
        var expectedSubscriptionId = resultEnvironmentSubscription as string == SierraIntegration
            ? EswDevOpsSdk.SierraIntegrationSubscriptionId
            : EswDevOpsSdk.GetSubscriptionId(deploymentEnvironmentName);

        var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(deploymentEnvironmentName);

        subscriptionId.Should().Be(expectedSubscriptionId);
    }

    [Fact, IsDev]
    public void GetSubscriptionId_works_for_known_environments()
    {
        var environmentNames = typeof(DeploymentEnvironment).GetFields().Select(x => (string)x.GetValue(null));
        foreach (var environmentName in environmentNames)
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName);
            var subscriptionId = EswDevOpsSdk.GetSubscriptionId();
            subscriptionId.Should().NotBeNullOrEmpty();
        }
    }

    [Theory, IsDev]
    [InlineData(DeploymentEnvironment.CI, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope })]
    [InlineData(DeploymentEnvironment.Prod, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope, DeploymentRegion.EastUS })]
    [InlineData(DeploymentEnvironment.Prod, DeploymentRegion.EastUS, new[] { DeploymentRegion.EastUS, DeploymentRegion.WestEurope })]
    public void GetRegionSequence_ForAllEnvironments(DeploymentEnvironment env, DeploymentRegion source, DeploymentRegion[] expected)
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

            EswDevOpsSdk.CreateDeploymentContext(DeploymentEnvironment.CI).PreferredRegions.Should().ContainInOrder("West Europe");
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
