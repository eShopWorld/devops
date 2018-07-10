namespace Eshopworld.DevOps
{
    using System;
    using System.IO;
    using System.Linq;
    using Eshopworld.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.KeyVault;
    using System.Collections.Generic;
    using Microsoft.Azure.Services.AppAuthentication;

    /// <summary>
    /// Top level pool of SDK related functionality offered as part of platform
    /// </summary>
    public static class EswDevOpsSdk
    {
        internal const string EnvironmentEnvVariable = "ASPNETCORE_ENVIRONMENT";
        internal const string DeploymentRegionEnvVariable = "DEPLOYMENT_REGION";
        internal const string KeyVaultConfigSourceUrlKey = "KeyVaultConfigSourceUrl";
        internal const string AADClientIdEnvVariable = "AAD_CLIENT_ID";
        internal const string AADClientSecretEnvVariable = "AAD_CLIENT_SECRET";

        private static readonly Dictionary<string, string[]> RegionFallbackMap = new Dictionary<string, string[]>
        {
            {Regions.WestEurope,          new[] {Regions.WestEurope,          Regions.EastUS,             Regions.AustraliaSouthEast, Regions.SoutheastAsia}},
            {Regions.EastUS,              new[] {Regions.EastUS,              Regions.AustraliaSouthEast, Regions.WestEurope,         Regions.SoutheastAsia}},
            {Regions.AustraliaSouthEast,  new[] {Regions.AustraliaSouthEast,  Regions.EastUS,             Regions.WestEurope,         Regions.SoutheastAsia}},
            {Regions.SoutheastAsia,       new[] {Regions.SoutheastAsia,       Regions.AustraliaSouthEast, Regions.EastUS,             Regions.WestEurope}}
        };

        /// <summary>
        /// simplified variant of full fledged method - <see cref="BuildConfiguration(string, string, bool)"/>
        /// </summary>
        /// <param name="useTest">true to force a .INTEGRATION.json optional configuration load, false otherwise.</param>
        /// <returns>configuration root instance</returns>
        public static IConfigurationRoot BuildConfiguration(bool useTest = false)
        {
            return BuildConfiguration(Environment.CurrentDirectory, GetEnvironmentVariable(EnvironmentEnvVariable),
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
        ///     #4 Load the optional KeyVault settings with connection details
        ///     #5 Try to get the Vaul setting from configuration
        ///     #6 If Vault details are present, load configuration from the target vault
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
            var vaultUrl = config["KeyVaultConfigSourceUrlKey"];

            if (!string.IsNullOrEmpty(vaultUrl))
            {
                configBuilder.AddAzureKeyVault(vaultUrl,
                    new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback)),
                    new SectionKeyVaultManager());
              
            }

            return configBuilder.Build();
        }

        /// <summary>
        /// create add context
        /// </summary>
        /// <returns></returns>
        public static IAADContext CreateAADContext()
        {
            // try to locate auth file in app
            var authFile = GetAuthFilePath();
            if (!string.IsNullOrWhiteSpace(authFile))
            {
                return new AADContext {AuthFilePath = authFile};
            }
            
            //fallback option - app id/secret from environment variables - resolve from process/user/machine (in that sequence)
            var clientEnvPair = ResolveAADEnvVariables(EnvironmentVariableTarget.Process) ??
                                ResolveAADEnvVariables(EnvironmentVariableTarget.User) ??
                                ResolveAADEnvVariables(EnvironmentVariableTarget.Machine);

            if (clientEnvPair != null)
            {
                return new AADContext
                {
                    ClientId = clientEnvPair.Item1,
                    ClientSecret = clientEnvPair.Item2,
                    TenantId = "3e14278f-8366-4dfd-bcc8-7e4e9d57f2c1",
                    SubscriptionId = GetSubscriptionId()
                };
            }

            return null; // even fallback found
        }

        /// <summary>
        /// retrieve deployment context
        /// </summary>
        /// <returns>deployment context instance</returns>
        public static DeploymentContext CreateDeploymentContext()
        {
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

        /// <summary>
        /// get auth file path
        /// 
        /// note that we only support one single file in the 
        /// </summary>
        /// <returns>auth file path or null</returns>
        private static string GetAuthFilePath()
        {
            var appLocalData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var eswLocalDataFolder = Path.Combine(appLocalData, "Eshopworld");
            var authFiles = Directory.GetFiles(eswLocalDataFolder, "*.azureauth");

            if (authFiles?.Length > 1)
            {
                throw new DevOpsSDKException($"Multiple AAD authentication file detected in {eswLocalDataFolder}. Only single file is supported.");
            }
            
            return authFiles?.FirstOrDefault();
        }

        private static string GetSubscriptionId()
        {
            var environmentName = Environment.GetEnvironmentVariable(EnvironmentEnvVariable);           

            switch (environmentName?.ToUpperInvariant())
            {
                case "CI":
                    return "30c09ef3-7f8a-4a13-a864-776438027e9d";
                case "DEVELOPMENT":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //this points to TEST
                case "PREPROD":
                    return "be155179-5691-45d1-a5d2-3d7dde0862b1";
                case "PRODUCTION":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //TODO: update when subscription becomes available
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

        private static Tuple<string, string> ResolveAADEnvVariables(EnvironmentVariableTarget target)
        {
            var clientIdVal = Environment.GetEnvironmentVariable(AADClientIdEnvVariable, target);
            if (!string.IsNullOrWhiteSpace(clientIdVal))
            {
                var clientSecretVal = Environment.GetEnvironmentVariable(AADClientSecretEnvVariable, target);
                if (string.IsNullOrWhiteSpace(clientSecretVal))
                {
                    throw new DevOpsSDKException(
                        $"{AADClientIdEnvVariable} variable found but no value exists for {AADClientSecretEnvVariable}");
                }

                return new Tuple<string, string>(clientIdVal, clientSecretVal);
            }

            return null;
        }

        private static class Regions
        {
            internal const string WestEurope = "West Europe";
            // ReSharper disable once InconsistentNaming
            internal const string EastUS = "East US";
            // ReSharper disable once InconsistentNaming
            internal const string AustraliaSouthEast = "Australia Southeast";
            internal const string SoutheastAsia = "Southeast Asia";
        }
    }
}
