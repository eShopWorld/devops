namespace Eshopworld.DevOps
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using JetBrains.Annotations;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Top level pool of SDK related functionality offered as part of platform
    /// </summary>
    public static class EswDevOpsSdk
    {
        internal const string EnvironmentEnvVariable = "ASPNETCORE_ENVIRONMENT";
        internal const string DeploymentRegionEnvVariable = "DEPLOYMENT_REGION";
        internal const string KeyVaultUrlKey = "KeyVaultUrl";
        internal const string SierraIntegrationSubscriptionId = "45d5ef37-02bc-4b3d-9e62-19c14f3b9603";
        private static readonly Dictionary<DeploymentRegion, DeploymentRegion[]> RegionSequenceMap = new Dictionary<DeploymentRegion, DeploymentRegion[]>
        {
            {DeploymentRegion.WestEurope,          new[] {DeploymentRegion.WestEurope,          DeploymentRegion.EastUS }},
            {DeploymentRegion.EastUS,              new[] {DeploymentRegion.EastUS,              DeploymentRegion.WestEurope }}
        };

        /// <summary>
        /// simplified variant of full fledged method - <see cref="BuildConfiguration(string, string, bool)"/>
        /// </summary>
        /// <param name="useTest">true to force a .INTEGRATION.json optional configuration load, false otherwise.</param>
        /// <returns>configuration root instance</returns>
        public static IConfigurationRoot BuildConfiguration(bool useTest = false)
        {
            return BuildConfiguration(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location),
                GetEnvironmentVariable(EnvironmentEnvVariable),
                useTest);
        }

        /// <summary>
        /// Builds the <see cref="ConfigurationBuilder"/> and retrieves all main config sections from the resulting
        ///     configuration.
        /// Under a test run, the release definition will rename the environment ex: appsettings.CI.json file for the target environment (CI)
        ///     to appsettings.TEST.json, so useTest will effectively load the right file.
        /// </summary>
        /// <param name="basePath">The base path to use when looking for the JSON settings files.</param>
        /// <param name="environment">The name of the environment to scan for environmental configuration, null to skip.</param>
        /// <param name="useTest">true to force a .INTEGRATION.json optional configuration load, false otherwise.</param>
        /// <returns>The configuration root after building the builder.</returns>
        /// <remarks>
        /// The configuration flow is:
        ///     #1 Get the default appsettings.json
        ///     #2 Get the environmental appsettings.{ENV}.json
        ///     #3 If it's a test, load the [optional] appsettings.TEST.json
        ///     #4 Try to get the Vault setting from configuration
        ///     #5 If Vault details are present, load configuration from the target vault
        /// </remarks>
        public static IConfigurationRoot BuildConfiguration(string basePath, string environment = null, bool useTest = false)
        {
            var configBuilder = new ConfigurationBuilder().SetBasePath(basePath)
                                                          .AddJsonFile("appsettings.json");

            if (!string.IsNullOrEmpty(environment))
            {
                configBuilder.AddJsonFile($"appsettings.{environment}.json");
            }

            if (useTest)
            {
                configBuilder.AddJsonFile("appsettings.TEST.json", optional: true);
                configBuilder.AddJsonFile("appsettings.INTEGRATION.json", optional: true);
            }

            configBuilder.AddEnvironmentVariables();

            var config = configBuilder.Build();
            var vaultUrl = config[KeyVaultUrlKey];

            if (!string.IsNullOrEmpty(vaultUrl))
            {
                configBuilder.AddAzureKeyVault(vaultUrl,
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                    new SectionKeyVaultManager());

            }

            return configBuilder.Build();
        }

        /// <summary>
        /// returns name of the environment retrieved from <see cref="EnvironmentEnvVariable"/> environment variable
        /// </summary>
        /// <returns>name of the environment</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static string GetEnvironmentName()
        {
            return GetEnvironmentVariable(EnvironmentEnvVariable);
        }

        /// <summary>
        /// retrieve deployment context
        /// </summary>
        /// <param name="targetEnvironment">name of the environment to target</param>
        /// <returns>deployment context instance</returns>
        public static DeploymentContext CreateDeploymentContext(string targetEnvironment = EnvironmentNames.PROD)
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

            foreach (var field in typeof(DeploymentRegion).GetFields().Where(fi=> !fi.IsSpecialName))
            {
                var regionDescriptor = (RegionDescriptorAttribute) field.GetCustomAttributes(
                    typeof(RegionDescriptorAttribute),
                    false).FirstOrDefault(); //but it will be there (see tests)

                if (regionDescriptor != null &&
                    value.Equals(regionDescriptor.ToString(), StringComparison.OrdinalIgnoreCase))
                    return (DeploymentRegion) field.GetRawConstantValue();
            }

            throw new DevOpsSDKException($"Unrecognized region name - {value}");
        }

        /// <summary>
        /// get region sequence for a combination of environment and current region
        /// </summary>
        /// <param name="environmentName">name of the environment</param>
        /// <param name="masterRegion">current region to get the sequence for</param>
        /// <returns>sequence of regions</returns>
        [NotNull]        
        // ReSharper disable once MemberCanBePrivate.Global
        public static IEnumerable<DeploymentRegion> GetRegionSequence(string environmentName, DeploymentRegion masterRegion)
        {
            if (string.IsNullOrEmpty(environmentName))
                throw new ArgumentException("Empty or null value", nameof(environmentName));

            if (EnvironmentNames.CI.Equals(environmentName, StringComparison.OrdinalIgnoreCase))
                return new[] { DeploymentRegion.WestEurope };
            
            //map region to hierarchy
            if (!RegionSequenceMap.ContainsKey(masterRegion))
            {
                throw new DevOpsSDKException($"Unrecognized value for region environmental variable - {masterRegion}");
            }

            return RegionSequenceMap[masterRegion];
        }

        /// <summary>
        /// Gets the subscription id of assigned to the current environment.
        /// </summary>
        /// <returns>Returns the subscription id.</returns>
        public static string GetSubscriptionId()
        {
            var environmentName = GetEnvironmentName();
            if (string.IsNullOrWhiteSpace(environmentName))
                throw new DevOpsSDKException($"No environment name set. Check {EnvironmentEnvVariable}");

            return GetSubscriptionId(environmentName);
        }

        internal static string GetSubscriptionId([NotNull] string environmentName)
        {
            if (environmentName == null) throw new ArgumentNullException(nameof(environmentName));

            switch (environmentName.ToUpperInvariant())
            {
                case EnvironmentNames.CI:
                    return "30c09ef3-7f8a-4a13-a864-776438027e9d";
                case EnvironmentNames.PREP:
                    return "be155179-5691-45d1-a5d2-3d7dde0862b1";
                case EnvironmentNames.PROD:
                    return "70969183-432d-45bf-9098-39433c6b2d12";
                case EnvironmentNames.SAND:
                    return "b40d6034-7393-4b8a-af29-4bf00d4b0a31";
                case EnvironmentNames.DEVELOPMENT:
                case EnvironmentNames.TEST:
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1";
                default:
                    throw new DevOpsSDKException($"Environment name {environmentName} is not valid.");
            }
        }

        /// <summary>
        /// Gets the subscription id assigned to the environment which is deployed.
        /// </summary>
        /// <param name="deploymentEnvironmentName">The name of environment to which deployment is performed.</param>
        /// <returns>Returns the subscription id.</returns>
        public static string GetSierraDeploymentSubscriptionId([NotNull] string deploymentEnvironmentName)
        {
            if (deploymentEnvironmentName == null) throw new ArgumentNullException(nameof(deploymentEnvironmentName));

            var environmentName = GetEnvironmentName();
            if (string.IsNullOrWhiteSpace(environmentName))
                throw new DevOpsSDKException($"No environment name set. Check {EnvironmentEnvVariable}");

            return EnvironmentNames.PROD.Equals(environmentName, StringComparison.OrdinalIgnoreCase)
                ? GetSubscriptionId(deploymentEnvironmentName)
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
