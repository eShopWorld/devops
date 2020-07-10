using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;

namespace Eshopworld.DevOps.AzureKeyVault
{
    public static class AzureKeyVaultConfigurationExtensions
    {
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            SecretClient client,
            IKeyVaultSecretManager manager = null)
        {
            if (configurationBuilder == null)
                throw new ArgumentNullException(nameof(configurationBuilder));

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            configurationBuilder.Add(new AzureKeyVaultConfigurationSource()
            {
                Client = client,
                Manager = manager ?? new DefaultKeyVaultSecretManager()
            });

            return configurationBuilder;
        }
    }
}
