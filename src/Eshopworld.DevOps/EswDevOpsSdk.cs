using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Azure.Identity;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// Top level pool of SDK related functionality offered as part of platform
    /// </summary>
    public static class EswDevOpsSdk
    {
        /// <summary>
        /// The name of an environment variable which defines the environment
        /// </summary>
        public const string EnvironmentEnvVariable = "ASPNETCORE_ENVIRONMENT";
        public const string SierraIntegrationSubscriptionId = "0b50e185-2e2a-4e1c-bf2f-ead0b80e0b79";

        internal const string DeploymentRegionEnvVariable = "DEPLOYMENT_REGION";
        internal const string KeyVaultUrlKey = "KEYVAULT_URL";

        private static readonly Dictionary<DeploymentRegion, DeploymentRegion[]> RegionSequenceMap =
            new Dictionary<DeploymentRegion, DeploymentRegion[]>
            {
                {
                    DeploymentRegion.WestEurope,
                    new[] {DeploymentRegion.WestEurope, DeploymentRegion.EastUS, DeploymentRegion.SoutheastAsia}
                },
                {
                    DeploymentRegion.EastUS,
                    new[] {DeploymentRegion.EastUS, DeploymentRegion.WestEurope, DeploymentRegion.SoutheastAsia}
                },
                {
                    DeploymentRegion.SoutheastAsia,
                    new[] {DeploymentRegion.SoutheastAsia, DeploymentRegion.EastUS, DeploymentRegion.WestEurope}
                }
            };

        /// <summary>
        /// simplified variant of full fledged method - <see cref="BuildConfiguration(string, string)"/>
        /// </summary>
        /// <returns>configuration root instance</returns>
        public static IConfigurationRoot BuildConfiguration()
        {
            return BuildConfiguration(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location),
                GetEnvironmentVariable(EnvironmentEnvVariable));
        }

        /// <summary>
        /// Builds the <see cref="IConfigurationRoot"/> using standard ESW configuration sorces (appsettings files, key vaults, environment variables).
        /// </summary>
        /// <param name="basePath">The base path to use when looking for the JSON settings files.</param>
        /// <param name="environment">The name of the environment to scan for environmental configuration, null to skip.</param>
        /// <returns>The configuration root.</returns>
        /// <remarks>
        /// The returned configuration is assembled from the following sources:
        ///     #1 the appsettings.json file (if it exists)
        ///     #2 the environmental appsettings.{ENV}.json file (if it exists and the <paramref name="environment"/> parameter is not empty)
        ///     #3 the application's key vault (if its address is defined using an environment variable or in mentioned above appsettings files)
        ///     #4 the environment variables
        ///     
        /// When an individual setting is defined in multiple places with different values, its last configuration source wins. It means any
        /// settings can be overwritten using environment variables e.g. during development.
        /// </remarks>
        public static IConfigurationRoot BuildConfiguration(string basePath, string environment = null)
        {
            var configBuilder = CreateInitialConfigurationBuilder()
                .AddEnvironmentVariables();
            var config = configBuilder.Build();

            var isVaultUriValid = Uri.TryCreate(config[KeyVaultUrlKey], UriKind.Absolute, out Uri vaultUri);
            if (!isVaultUriValid)
                return config;

            var kvConfigBuilder = CreateInitialConfigurationBuilder()
                .AddAzureKeyVault(vaultUri, new DefaultAzureCredential())
                .AddEnvironmentVariables();

            return kvConfigBuilder.Build();

            IConfigurationBuilder CreateInitialConfigurationBuilder()
            {
                var builder = new ConfigurationBuilder();
                builder.SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true);
                if (!string.IsNullOrEmpty(environment))
                    builder.AddJsonFile($"appsettings.{environment}.json", optional: true);

                return builder;
            }
        }

        /// <summary>
        /// returns name of the environment retrieved from <see cref="EnvironmentEnvVariable"/> environment variable
        /// </summary>
        /// <returns>The name of the environment (might be empty or null).</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static string GetEnvironmentName()
        {
            return GetEnvironmentVariable(EnvironmentEnvVariable);
        }

        /// <summary>
        /// Returns the environment (as defined by <see cref="EnvironmentEnvVariable"/> environment variable)
        /// </summary>
        /// <returns>The environment.</returns>
        /// <exception cref="DevOpsSDKException">The environment variable is missing or its value is invalid.</exception>
        public static DeploymentEnvironment GetEnvironment()
        {
            var name = GetEnvironmentName();
            if (Enum.TryParse<DeploymentEnvironment>(name, true, out var environment))
            {
                return environment;
            }

            if (string.IsNullOrWhiteSpace(name))
                throw new DevOpsSDKException($"The environment variable {EnvironmentEnvVariable} is missing or its value is empty.");

            throw new DevOpsSDKException($"The environment variable {EnvironmentEnvVariable} contains value '{name}' is not a valid environment name.");
        }

        /// <summary>
        /// retrieve deployment context
        /// </summary>
        /// <param name="targetEnvironment">the environment to target</param>
        /// <returns>deployment context instance</returns>
        public static DeploymentContext CreateDeploymentContext(DeploymentEnvironment targetEnvironment = DeploymentEnvironment.Prod)
        {
            var regionString = GetEnvironmentVariable(DeploymentRegionEnvVariable);

            if (string.IsNullOrWhiteSpace(regionString))
                throw new DevOpsSDKException(
                    $"Could not find deployment region environment variable. Please make sure that {DeploymentRegionEnvVariable} environment variable exists and has value");

            var parsed = ParseRegionFromString(regionString);

            var preferredRegions = GetRegionSequence(targetEnvironment, parsed)
                .Select(i => i.ToRegionName());

            return new DeploymentContext { PreferredRegions = preferredRegions };
        }

        private static DeploymentRegion ParseRegionFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Null or empty value", nameof(value));

            foreach (var field in typeof(DeploymentRegion).GetFields().Where(fi => !fi.IsSpecialName))
            {
                var regionDescriptor = (RegionDescriptorAttribute)field.GetCustomAttributes(
                    typeof(RegionDescriptorAttribute),
                    false).First();

                if (value.Equals(regionDescriptor.ToString(), StringComparison.OrdinalIgnoreCase))
                    return (DeploymentRegion)field.GetRawConstantValue();
            }

            throw new DevOpsSDKException($"Unrecognized region name - {value}");
        }

        /// <summary>
        /// Get region sequence for a combination of environment and current region
        /// </summary>
        /// <param name="environment">the environment</param>
        /// <param name="masterRegion">current region to get the sequence for</param>
        /// <returns>sequence of regions</returns>
        [NotNull]
        // ReSharper disable once MemberCanBePrivate.Global
        public static IEnumerable<DeploymentRegion> GetRegionSequence(DeploymentEnvironment environment, DeploymentRegion masterRegion)
        {
            if (environment == DeploymentEnvironment.CI)
                return new[] { DeploymentRegion.WestEurope };

            //map region to hierarchy
            if (!RegionSequenceMap.ContainsKey(masterRegion))
            {
                throw new DevOpsSDKException($"Unrecognized value for region environmental variable - {masterRegion}");
            }

            var map = RegionSequenceMap[masterRegion];
            return (environment == DeploymentEnvironment.Sand || environment == DeploymentEnvironment.Test ||
                    environment == DeploymentEnvironment.Development)
                ? map.Where(r => r != DeploymentRegion.SoutheastAsia)
                : map;
        }

        /// <summary>
        /// Gets the subscription id of assigned to the current environment.
        /// </summary>
        /// <returns>Returns the subscription id.</returns>
        public static string GetSubscriptionId()
        {
            return GetSubscriptionId(GetEnvironment());
        }

        internal static string GetSubscriptionId(DeploymentEnvironment environment)
        {
            switch (environment)
            {
                case DeploymentEnvironment.CI:
                    return "30c09ef3-7f8a-4a13-a864-776438027e9d";
                case DeploymentEnvironment.Prep:
                    return "be155179-5691-45d1-a5d2-3d7dde0862b1";
                case DeploymentEnvironment.Prod:
                    return "70969183-432d-45bf-9098-39433c6b2d12";
                case DeploymentEnvironment.Sand:
                    return "b40d6034-7393-4b8a-af29-4bf00d4b0a31";
                case DeploymentEnvironment.Development:
                case DeploymentEnvironment.Test:
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, $"Environment {environment} is not valid.");
            }
        }

        /// <summary>
        /// Gets the subscription id assigned to the environment which is deployed.
        /// </summary>
        /// <param name="deploymentEnvironment">The environment to which deployment is performed.</param>
        /// <returns>Returns the subscription id.</returns>
        public static string GetSierraDeploymentSubscriptionId(DeploymentEnvironment deploymentEnvironment)
        {
            return DeploymentEnvironment.Prod == GetEnvironment()
                ? GetSubscriptionId(deploymentEnvironment)
                : SierraIntegrationSubscriptionId;
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        }
    }
}
