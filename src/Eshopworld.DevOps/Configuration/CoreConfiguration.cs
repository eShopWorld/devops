using Microsoft.Extensions.Configuration;

namespace Eshopworld.DevOps.Configuration
{
    /// <summary>
    /// Core configuration builder implementation with specific loading sequence built in
    /// </summary>
    public static class CoreConfiguration
    {
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
        public static IConfigurationRoot Build(string basePath, string environment = null, bool useTest = false)
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
                // TODO: when packaged, improve this by binding a POCO (inside the package) to it and add a section to the settings names
                configBuilder.AddAzureKeyVault(
                    $"https://{vault}.vault.azure.net/",
                    config["KeyVaultClientId"],
                    config["KeyVaultClientSecret"],
                    new SectionKeyVaultManager());

                // TODO: once we have scanning and this code wired together, need to re-evaluate
            }

            return configBuilder.Build();
        }
    }

    // TODO: add reload logic through a trasparent timer
    // TODO: once in KV, reload logic should be added to remove the need for restarts and needs to be written since KV doesn't have reload functionality built into it's package
}
