// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    using Azure.KeyVault;
    using Azure.KeyVault.Models;
    using Azure.Services.AppAuthentication;
    using Eshopworld.DevOps;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    /// <summary>Class Configuration extensions.</summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Binds the base section of the config to an actual class of type T.
        /// Config options with a dash in the name will have the dash dropped so it can bind to the poco class properties,
        /// e.g. "My-Key-1" will become "MyKey1" so it can bind to a class property named `MyKey1`
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>IConfigurationSection.</returns>
        public static T BindBaseSection<T>(this IConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException( nameof(config), "Configuration must be set");

            var configBase = new ConfigurationBuilder();
            var items = new Dictionary<string, string>();

            foreach (var c in config.GetChildren().Where(c => c.Value != null))
            {
                // We replace "--" with ":" so as that is how we denote our sub items in key vault, it is therefore converted
                // into the colon so it can be split appropriately in the standard way.
                var key = c.Key.Replace("--", ":", StringComparison.InvariantCulture);

                var parts = key.Split('-');
                if (parts.Length > 1)
                {
                    var safeKey = $"base:{string.Join(string.Empty, parts)}";
                    items.Add(safeKey, c.Value);
                }
                items.Add($"base:{key}", c.Value);
            }

            configBase.AddInMemoryCollection(items);
            return configBase.Build().GetSection("base").Get<T>();
        }

        /// <summary>
        /// Uses the desired default configurations.  Environment taken from EnvVariable "ENVIRONMENT" if not passed.
        /// Builds configuration sources in the following order:
        /// - 1. Environment variables
        /// - 2. Command line arguments
        /// - 3. Json file (appsettings.json, followed by appsettings.{env}.json)
        /// Note:
        /// - appsettings.{env}.json WILL override appsettings.json file settings.
        /// </summary>
        /// <param name="builder">The configuration builder to bind to.</param>
        /// <param name="appSettingsPath">The application settings path.</param>
        /// <param name="environment">Specify the environment - optional, as its loaded from the ENVIRONMENT env variable if not set here.</param>
        /// <returns>The configuration builder after config has been added.</returns>
        public static IConfigurationBuilder UseDefaultConfigs(this IConfigurationBuilder builder, string appSettingsPath = "appsettings.json", string environment = null)
        {
            builder.AddEnvironmentVariables()
                    .AddCommandLine(Environment.GetCommandLineArgs())
                    .AddJsonFile(appSettingsPath, true);

            var env = EswDevOpsSdk.GetEnvironmentName();

            if (!string.IsNullOrEmpty(environment))
                env = environment;

            if (!string.IsNullOrEmpty(env))
            {
                builder.AddJsonFile($"appsettings.{env}.json", true, true);
            }

            return builder;
        }

        /// <summary>Adds the key vault secrets specified.  Uses Msi auth and gets the Key Vault url from `EswDevOpsSdk.KeyVaultUrlKey` setting.  If url is not set, it falls back to "KeyVaultInstanceName" setting as backup.</summary>
        /// <param name="builder">The builder to extend.</param>
        /// <param name="params">The list of keys to load.</param>
        /// <returns>IConfigurationBuilder with param keys as settings.</returns>
        /// <exception cref="InvalidOperationException">Vault url must be set, ensure `EswDevOpsSdk.KeyVaultUrlKey` is set or "KeyVaultInstanceName" has been set in config</exception>
        public static IConfigurationBuilder AddKeyVaultSecrets(this IConfigurationBuilder builder, params string[] @params)
        {
            // Get the expected key vault url setting from the environment.
            var vaultUrl = builder.GetValue<string>(EswDevOpsSdk.KeyVaultUrlKey);

            if (string.IsNullOrEmpty(vaultUrl))
            {
                // If url was not set, look for an instance name and infer url.
                var instanceName = builder.GetValue<string>("KeyVaultInstanceName");
                vaultUrl = $"https://{instanceName}.vault.azure.net";
            }

            // Verify the key vault url is set.
            if (string.IsNullOrEmpty(vaultUrl))
            {
                throw new InvalidOperationException($"Vault url must be set, ensure \"{EswDevOpsSdk.KeyVaultUrlKey}\" or \"KeyVaultInstanceName\" have been set in config");
            }

            // Verify the key vault url is a valid url.
            if (!(Uri.TryCreate(vaultUrl, UriKind.Absolute, out var kvUri)))
            {
                throw new InvalidOperationException($"Vault url \"{vaultUrl}\" is invalid");
            }

            return AddKeyVaultSecrets(builder, kvUri, @params);
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
        /// <exception cref="ArgumentException">Vault url must be set</exception>
        /// <exception cref="InvalidOperationException">Problem occurred retrieving secrets from KeyVault using Managed Identity</exception>
        public static IConfigurationBuilder AddKeyVaultSecrets(this IConfigurationBuilder builder, Uri vaultUrl, IEnumerable<string> keys, bool suppressKeyNotFoundError = true)
        {
            if (vaultUrl == null)
                throw new ArgumentNullException(nameof(vaultUrl), "Vault url must be set");

            if (!keys.Any())
                return builder;

            try
            {
                var vault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new AzureServiceTokenProvider().KeyVaultTokenCallback));
                var secrets = new List<KeyValuePair<string, string>>();

                // Gather secrets from Key Vault, one by one.
                foreach (var key in keys)
                {
                    try
                    {
                        var secret = vault.GetSecretAsync(vaultUrl.AbsoluteUri, key).ConfigureAwait(false).GetAwaiter().GetResult();
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
        public static IConfigurationBuilder AddValues(this IConfigurationBuilder builder, IDictionary<string, string> values)
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
