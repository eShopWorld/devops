using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eshopworld.DevOps.AzureKeyVault
{
    public class AzureKeyVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly SecretClient _client;
        private readonly IKeyVaultSecretManager _manager;

        public AzureKeyVaultConfigurationProvider(SecretClient client, IKeyVaultSecretManager manager)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var secretProperties in _client.GetPropertiesOfSecretsAsync())
            {
                if (!_manager.ShouldLoad(secretProperties) || secretProperties?.Enabled != true)
                    continue;

                var secret = await _client.GetSecretAsync(secretProperties.Name).ConfigureAwait(false);
                var key = _manager.GetKey(secret.Value);
                Data.Add(key, secret.Value.Value);
            }

            Data = data;
        }
    }
}
