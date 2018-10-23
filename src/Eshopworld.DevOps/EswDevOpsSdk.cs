namespace Eshopworld.DevOps
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.KeyVault;
    using System.Collections.Generic;
    using Microsoft.Azure.Services.AppAuthentication;
    using System.Reflection;

    /// <summary>
    /// Top level pool of SDK related functionality offered as part of platform
    /// </summary>
    public static class EswDevOpsSdk
    {
        internal const string EnvironmentEnvVariable = "ASPNETCORE_ENVIRONMENT";
        internal const string DeploymentRegionEnvVariable = "DEPLOYMENT_REGION";
        internal const string KeyVaultUrlKey = "KeyVaultUrl";

        // ReSharper disable once InconsistentNaming
        public const string CI_EnvironmentName = "CI";
        // ReSharper disable once InconsistentNaming
        public const string SAND_EnvironmentName = "SAND";
        // ReSharper disable once InconsistentNaming
        public const string TEST_EnvironmentName = "TEST";
        // ReSharper disable once InconsistentNaming
        public const string PREP_EnvironmentName = "PREP";
        // ReSharper disable once InconsistentNaming
        public const string PROD_EnvironmentName = "PROD";
        

        private static readonly Dictionary<string, string[]> RegionFallbackMap = new Dictionary<string, string[]>
        {
            {Regions.WestEurope,          new[] {Regions.WestEurope,          Regions.EastUS     }},
            {Regions.EastUS,              new[] {Regions.EastUS,              Regions.WestEurope }}
        };

        public static string[] RegionList = new[] {Regions.WestEurope, Regions.EastUS};

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
        public static string GetEnvironmentName()
        {
            return GetEnvironmentVariable(EnvironmentEnvVariable);
        }

        /// <summary>
        /// retrieve deployment context
        /// </summary>
        /// <param name="targetEnvironment">name of the environment to target</param>
        /// <returns>deployment context instance</returns>
        public static DeploymentContext CreateDeploymentContext(string targetEnvironment = PROD_EnvironmentName)
        {
            if (CI_EnvironmentName.Equals(targetEnvironment, StringComparison.OrdinalIgnoreCase))
                return new DeploymentContext {PreferredRegions = new [] {Regions.WestEurope}};

            var region = GetEnvironmentVariable(DeploymentRegionEnvVariable);

            if (string.IsNullOrWhiteSpace(region))
                throw new InvalidOperationException(
                    $"Could not find deployment region environment variable. Please make sure that {DeploymentRegionEnvVariable} environment variable exists and has value");
            
            //map region to hierarchy
            if (!RegionFallbackMap.ContainsKey(region))
            {
                throw new DevOpsSDKException($"Unrecognized value for region environmental variable - {region}");
            }

            return new DeploymentContext {PreferredRegions = RegionFallbackMap[region]};
        }
        public static string GetSubscriptionId()
        {
            var environmentName = GetEnvironmentName();           

            switch (environmentName?.ToUpperInvariant())
            {
                case "CI":
                    return "30c09ef3-7f8a-4a13-a864-776438027e9d";
                case "DEVELOPMENT":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //this points to TEST
                case "PREP":
                    return "be155179-5691-45d1-a5d2-3d7dde0862b1";
                case "PROD":
                    return "70969183-432d-45bf-9098-39433c6b2d12"; 
                case "SAND":
                    return "b40d6034-7393-4b8a-af29-4bf00d4b0a31";
                case "TEST":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1";
                default:
                    throw new DevOpsSDKException($"No environment name set. Check {EnvironmentEnvVariable}");
            }
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
                   ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        }

        private static class Regions
        {
            internal const string WestEurope = "West Europe";
            // ReSharper disable once InconsistentNaming
            internal const string EastUS = "East US";           
        }
    }
}
