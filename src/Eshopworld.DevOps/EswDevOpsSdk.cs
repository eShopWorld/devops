using System;
using System.IO;
using System.Linq;
using Eshopworld.Core;
using Microsoft.Extensions.Configuration;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// Top level pool of SDK related functionality offered as part of platform
    /// </summary>
    public static class EswDevOpsSdk
    {
        internal const string EnvironmentEnvVariable = "ASPNETCORE_ENVIRONMENT";
        internal const string AADClientIdEnvVariable = "AAD_CLIENT_ID";
        internal const string AADClientSecretEnvVariable = "AAD_CLIENT_SECRET";

        /// <summary>
        /// Builds the <see cref="ConfigurationBuilder"/> and retrieves all main config sections from the resulting
        ///     configuration.
        /// Under a test run, the release definition will rename the environment ex: appsettings.CI.json file for the target environment (CI)
        ///     to appsettings.TEST.json, so useTest will effectively load the right file.
        /// </summary>
        /// <param name="basePath">The base path to use when looking for the JSON settings files.</param>
        /// <param name="environment">The name of the environment to scan for environmental configuration, null to skip.</param>
        /// <param name="useTest">true to force a .TEST.json optional configuration load, false otherwise.</param>
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
            }

            configBuilder.AddJsonFile("appsettings.KV.json", optional: true);
            configBuilder.AddEnvironmentVariables();

            var config = configBuilder.Build();
            var vault = config["KeyVaultName"];

            if (!string.IsNullOrEmpty(vault))
            {
                configBuilder.AddAzureKeyVault(
                    $"https://{vault}.vault.azure.net/",
                    config["KeyVaultClientId"],
                    config["KeyVaultClientSecret"],
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
            var appLocalData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var authFile = Directory.GetFiles(appLocalData, "*.azureauth").FirstOrDefault();
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

        private static string GetSubscriptionId()
        {
            var environmentName = Environment.GetEnvironmentVariable(EnvironmentEnvVariable);           

            switch (environmentName?.ToUpperInvariant())
            {
                case "CI":
                    return "30c09ef3-7f8a-4a13-a864-776438027e9d";
                case "DEVELOPMENT":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; 
                case "PREPROD":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //TODO: update when subscription becomes available
                case "PRODUCTION":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //TODO: update when subscription becomes available
                case "SAND":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //TODO: update when subscription becomes available
                case "TEST":
                    return "49c77085-e8c5-4ad2-8114-1d4e71a64cc1"; //TODO: update when subscription becomes available
                default:
                    throw new InvalidOperationException($"No environment name set. Check {EnvironmentEnvVariable}");
            }
        }

        // ReSharper disable once InconsistentNaming
        private static Tuple<string, string> ResolveAADEnvVariables(EnvironmentVariableTarget target)
        {
            var clientIdVal = Environment.GetEnvironmentVariable(AADClientIdEnvVariable, target);
            if (!string.IsNullOrWhiteSpace(clientIdVal))
            {
                var clientSecretVal = Environment.GetEnvironmentVariable(AADClientSecretEnvVariable, target);
                if (string.IsNullOrWhiteSpace(clientSecretVal))
                {
                    throw new InvalidOperationException(
                        $"{AADClientIdEnvVariable} variable found but no value exists for {AADClientSecretEnvVariable}");
                }

                return new Tuple<string, string>(clientIdVal, clientSecretVal);
            }

            return null;
        }
    }
}
