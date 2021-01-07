using System;
using System.IO;
using System.Reflection;
using Eshopworld.DevOps;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once InconsistentNaming
// ReSharper disable once CheckNamespace
public class EswDevOpsSdkTests
{
    [Fact, IsLayer0]
    public void BuildConfiguration_ReadFromCoreAppSettings()
    {
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, "ENV1", EnvironmentVariableTarget.Process); //process level is fine here
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["KeyRootAppSettings"].Should().BeEquivalentTo("AppSettingsValue");
    }

    [Fact, IsLayer0]
    public void BuildConfiguration_EnvironmentOverwrites()
    {
        Environment.SetEnvironmentVariable("OPTION2", "fromEnv", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("OPTION5", "fromEnv", EnvironmentVariableTarget.Process);
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "ORDER");

        sut["Option1"].Should().BeEquivalentTo("fromOrderAppSetting");
        sut["Option2"].Should().BeEquivalentTo("fromEnv");
        sut["Option3"].Should().BeEquivalentTo("value3");
        sut["Option4"].Should().BeEquivalentTo("fromKV");
        sut["Option5"].Should().BeEquivalentTo("fromENV");
    }

    [Fact, IsLayer0]
    public void BuildConfiguration_NonTestMode()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["KeyTestAppSettings"].Should().BeNullOrEmpty();
    }

    [Fact, IsLayer0]
    public void BuildConfiguration_ReadFromEnvironmentalAppSettings()

    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "ENV1");

        sut["KeyENV1AppSettings"].Should().BeEquivalentTo("ENV1AppSettingsValue");
    }

    [Fact, IsLayer0]
    public void BuildConfiguration_ReadFromEnvironmentalVariable()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory);

        sut["PATH"].Should().NotBeNullOrEmpty();
    }

    [Fact, IsLayer1]
    public void BuildConfiguration_MSIAuthenticationTest()
    {
        var sut = EswDevOpsSdk.BuildConfiguration(AssemblyDirectory, "CI");
        sut["keyVaultItem"].Should().Be("keyVaultItemValue");
    }

    private const string SierraIntegration = "si";

    [Theory, IsLayer0]
    [InlineData("CI", DeploymentEnvironment.CI)]
    [InlineData("PREP", DeploymentEnvironment.Prep)]
    [InlineData("pRep", DeploymentEnvironment.Prep)]
    public void GeEnvironmentTest(string envValue, DeploymentEnvironment env)
    {
        var prevEnv = Environment.GetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, envValue, EnvironmentVariableTarget.Process);
        try
        {
            var currentEnvironment = EswDevOpsSdk.GetEnvironment();
            currentEnvironment.Should().Be(env);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, prevEnv, EnvironmentVariableTarget.Process);
        }
    }

    [Theory, IsLayer0]
    [InlineData("West Europe", DeploymentRegion.WestEurope)]
    [InlineData("Southeast Asia", DeploymentRegion.SoutheastAsia)]
    [InlineData("East US", DeploymentRegion.EastUS)]
    [InlineData(null, DeploymentRegion.None)]
    public void GetDeploymentRegionTest(string regionValue, DeploymentRegion region)
    {
        var prevRegion = Environment.GetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable);
        Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, regionValue, EnvironmentVariableTarget.Process);
        try
        {
            EswDevOpsSdk.TryGetDeploymentRegion(out var deploymentRegion);
            deploymentRegion.Should().Be(region);
        }
        finally
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.DeploymentRegionEnvVariable, prevRegion, EnvironmentVariableTarget.Process);
        }
    }

    [Theory, IsLayer0]
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
        Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName.ToString(), EnvironmentVariableTarget.Process);
        var expectedSubscriptionId = resultEnvironmentSubscription as string == SierraIntegration
            ? EswDevOpsSdk.SierraIntegrationSubscriptionId
            : EswDevOpsSdk.GetSubscriptionId(deploymentEnvironmentName);

        var subscriptionId = EswDevOpsSdk.GetSierraDeploymentSubscriptionId(deploymentEnvironmentName);

        subscriptionId.Should().Be(expectedSubscriptionId);
    }

    [Fact, IsLayer0]
    public void GetSubscriptionId_works_for_known_environments()
    {
        var environmentNames = Enum.GetNames(typeof(DeploymentEnvironment));
        foreach (var environmentName in environmentNames)
        {
            Environment.SetEnvironmentVariable(EswDevOpsSdk.EnvironmentEnvVariable, environmentName, EnvironmentVariableTarget.Process);
            var subscriptionId = EswDevOpsSdk.GetSubscriptionId();
            subscriptionId.Should().NotBeNullOrEmpty();
        }
    }

    [Theory, IsLayer0]
    [InlineData(DeploymentEnvironment.CI, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope })]
    [InlineData(DeploymentEnvironment.Prod, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope, DeploymentRegion.EastUS, DeploymentRegion.SoutheastAsia })]
    [InlineData(DeploymentEnvironment.Prod, DeploymentRegion.EastUS, new[] { DeploymentRegion.EastUS, DeploymentRegion.WestEurope, DeploymentRegion.SoutheastAsia })]
    [InlineData(DeploymentEnvironment.Sand, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope, DeploymentRegion.EastUS })]
    [InlineData(DeploymentEnvironment.Sand, DeploymentRegion.EastUS, new[] { DeploymentRegion.EastUS, DeploymentRegion.WestEurope })]
    [InlineData(DeploymentEnvironment.Test, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope, DeploymentRegion.EastUS })]
    [InlineData(DeploymentEnvironment.Test, DeploymentRegion.EastUS, new[] { DeploymentRegion.EastUS, DeploymentRegion.WestEurope })]
    [InlineData(DeploymentEnvironment.Development, DeploymentRegion.WestEurope, new[] { DeploymentRegion.WestEurope, DeploymentRegion.EastUS })]
    [InlineData(DeploymentEnvironment.Development, DeploymentRegion.EastUS, new[] { DeploymentRegion.EastUS, DeploymentRegion.WestEurope })]
    public void GetRegionSequence_ForAllEnvironments(DeploymentEnvironment env, DeploymentRegion source, DeploymentRegion[] expected)
    {
        var ret = EswDevOpsSdk.GetRegionSequence(env, source);
        ret.Should().ContainInOrder(expected).And.HaveCount(expected.Length);
    }
    public class CreateDeploymentContext
    {
        [Theory, IsDev]
        [InlineData("West Europe", new[] { "West Europe", "East US", "Southeast Asia" })]
        [InlineData("East US", new[] { "East US", "West Europe", "Southeast Asia" })]
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
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
