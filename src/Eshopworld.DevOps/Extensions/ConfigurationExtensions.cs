namespace Microsoft.Extensions.Configuration
{
    using Azure.KeyVault.Models;
    using Azure.KeyVault;
    using Azure.Services.AppAuthentication;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    /// <summary>Class Configuration extensions.</summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Binds the base section of the config to an actual class of type T.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>IConfigurationSection.</returns>
        public static T BindBaseSection<T>(this IConfiguration config)
        {
            if (config == null)
                throw new ArgumentException("Configuration must be set", nameof(config));

            var configBase = new ConfigurationBuilder();
            configBase.AddInMemoryCollection(config.GetChildren().Where(c => c.Value != null).Select(c => new KeyValuePair<string, string>("base:" + c.Key, c.Value)));
            return configBase.Build().GetSection("base").Get<T>();
        }

        /// <summary>
        /// Adds the kubernetes secrets config.  Reads from a directory where it takes the file name as the key (config property) and the
        /// value is the content within the file.
        /// </summary>
        /// <param name="builder">The configuration builder to bind to.</param>
        /// <param name="path">The path to the directory containing the secrets (defaults to "secrets").</param>
        /// <param name="optional">if set to <c>true</c>, ignore if does not exist [optional].</param>
        /// <returns>The configuration builder after config has been added.</returns>
        public static IConfigurationBuilder AddKubernetesSecrets(this IConfigurationBuilder builder, string path = null, bool optional = true)
        {
            // Default path if not set.
            path ??= "/etc/secrets";

            // Default return if we cant find this folder to avoid runtime errors.  Worst case scenario
            // is that the settings are not loaded from here.
            if (!Directory.Exists(path))
            {
                return builder;
            }

            return builder.AddKeyPerFile(path, optional);
        }

        /// <summary>
        /// Uses the desired default configurations.  Environment taken from EnvVariable "ENVIRONMENT" if not passed.
        /// Builds configuration sources in the following order:
        /// - 1. Kubernetes Secrets (looks in the "etc/secrets" folder by default)
        /// - 2. Environment variables
        /// - 3. Command line arguments
        /// - 4. Json file (appsettings.json, followed by appsettings.{env}.json)
        /// Note:
        /// - appsettings.{env}.json WILL override appsettings.json file settings.
        /// </summary>
        /// <param name="builder">The configuration builder to bind to.</param>
        /// <param name="appSettingsPath">The application settings path.</param>
        /// <param name="environment">Specify the environment - optional, as its loaded from the ENVIRONMENT env variable if not set here.</param>
        /// <param name="kubernetesSecretsPath">The K8S secrets path.</param>
        /// <returns>The configuration builder after config has been added.</returns>
        public static IConfigurationBuilder UseDefaultConfigs(this IConfigurationBuilder builder, string appSettingsPath = "appsettings.json", string kubernetesSecretsPath = null, string environment = null)
        {
            builder.AddKubernetesSecrets(kubernetesSecretsPath)
                    .AddEnvironmentVariables()
                    .AddCommandLine(Environment.GetCommandLineArgs())
                    .AddJsonFile(appSettingsPath, true);

            var env = Environment.GetEnvironmentVariable("ENVIRONMENT");

            if (!string.IsNullOrEmpty(environment))
                env = environment;

            if (!string.IsNullOrEmpty(env))
            {
                builder.AddJsonFile($"appsettings.{env}.json", true, true);
            }

            return builder;
        }

        /// <summary>Adds the key vault secrets specified.  Uses Msi auth and gets the Key Vault url from "KEYVAULT_URL" setting.  If "KEYVAULT_URL" is not set, looks for "KeyVaultInstanceName" as backup.</summary>
        /// <param name="builder">The builder to extend.</param>
        /// <param name="params">The list of keys to load.</param>
        /// <returns>IConfigurationBuilder with param keys as settings.</returns>
        /// <exception cref="InvalidOperationException">Vault url must be set, ensure "KEYVAULT_URL" or "KeyVaultInstanceName" has been set in config</exception>
        public static IConfigurationBuilder AddKeyVaultSecrets(this IConfigurationBuilder builder, params string[] @params)
        {
            // Get the expected keyvault url setting from the environment.
            var vaultUrl = builder.GetValue<string>("KEYVAULT_URL");

            if (string.IsNullOrEmpty(vaultUrl))
            {
                // If url was not set, look for an instance name and infer url.
                var instanceName = builder.GetValue<string>("KeyVaultInstanceName");
                vaultUrl = $"https://{instanceName}.vault.azure.net";
            }

            if (string.IsNullOrEmpty(vaultUrl))
                throw new ArgumentException("Vault url must be set, ensure \"KEYVAULT_URL\" or \"KeyVaultInstanceName\" has been set in config", nameof(vaultUrl));

            return AddKeyVaultSecrets(builder, new Uri(vaultUrl), @params);
        }

        /// <summary>
        /// Adds the key vault secrets specified.  Uses Msi auth and builds the instance name on the fly.
        /// Needs config value "KeyVaultInstanceName" to work.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="vaultUrl">Key vault url to connect to.</param>
        /// <param name="keys">The list of keys to load.</param>
        /// <param name="suppressKeyNotFoundError">If [true], when a key is missing an invalid operation exception will be thrown. If [false], the
        /// error will be suppressed and it will just not add the key to the returned collection.</param>
        /// <returns>IConfigurationBuilder.</returns>
        /// <exception cref="InvalidOperationException">
        /// Expecting setting "KeyVaultInstanceName" to infer instance name
        /// or
        /// Problem occurred retrieving secrets from KeyVault using Managed Identity
        /// </exception>
        public static IConfigurationBuilder AddKeyVaultSecrets(this IConfigurationBuilder builder, Uri vaultUrl, IEnumerable<string> keys, bool suppressKeyNotFoundError = true)
        {
            try
            {
                if (vaultUrl == null)
                    throw new ArgumentException("Vault url must be set", nameof(vaultUrl));

                if (keys == default)
                    return builder;

                var vault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
                var secrets = new List<KeyValuePair<string, string>>();

                // Gather secrets from Key Vault, one by one.
                foreach (var key in keys)
                {
                    try
                    {
                        var secret = vault.GetSecretAsync(vaultUrl.AbsolutePath, key).ConfigureAwait(false).GetAwaiter().GetResult();
                        secrets.Add(new KeyValuePair<string, string>(key, secret.Value));
                    }
                    catch (KeyVaultErrorException e)
                        when (e.Response.StatusCode == HttpStatusCode.NotFound && suppressKeyNotFoundError)
                    {
                        // Do nothing if it fails to find the value.
                        Console.WriteLine($"Failed to find key vault setting: {key}, exception: {e.Message}");
                    }
                }

                // Add them to config.
                if (secrets.Any())
                    builder.AddInMemoryCollection(secrets);

                // Return updated builder.
                return builder;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Problem occurred retrieving secrets from KeyVault using Managed Identity", ex);
            }
        }

        /// <summary>
        /// Add key/value to config builder.
        /// </summary>
        /// <param name="builder">Builder to extend.</param>
        /// <param name="key">Key for value being added.</param>
        /// <param name="value">Value to add.</param>
        /// <returns>Builder with key/value added.</returns>
        public static IConfigurationBuilder AddValue(this IConfigurationBuilder builder, string key, string value)
        {
            return builder.AddInMemoryCollection(new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(key, value)});
        }

        /// <summary>
        /// Add enumerable list of config values.
        /// </summary>
        /// <param name="builder">Builder to extend.</param>
        /// <param name="values">List of values to add.</param>
        /// <returns>Builder with values added.</returns>
        public static IConfigurationBuilder AddValues(this IConfigurationBuilder builder, IEnumerable<KeyValuePair<string, string>> values)
        {
            return builder.AddInMemoryCollection(values);
        }

        /// <summary>
        /// Extension to grab values from existing configs during the build process.
        /// </summary>
        /// <typeparam name="T">Type of config object being pulled.</typeparam>
        /// <param name="builder">The builder being extended.</param>
        /// <param name="key">The key for the config value to search for.</param>
        /// <returns>T config value.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static T GetValue<T>(this IConfigurationBuilder builder, string key)
        {
            if (builder == null)
                throw new ArgumentException("Configuration builder must be set", nameof(builder));

            return builder.Build().GetValue<T>(key);
        }

        /// <summary>
        /// Gets value from config based on key.
        /// </summary>
        /// <param name="config">The configuration to get value from.</param>
        /// <param name="key">The unique key which holds the wanted value.</param>
        /// <param name="value">Out variable which returns found value if present.</param>
        /// <returns>bool, true or false depending on if the value associated with the key could be found.</returns>
        public static bool TryGetValue<T>(this IConfiguration config, string key, out T value)
        {
            key ??= "";

            value = config.GetValue<T>(key);
            if (value == null)
            {
                return false;
            }

            return true;
        }
    }
}
