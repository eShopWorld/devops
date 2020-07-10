using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Eshopworld.DevOps.AzureKeyVault
{
    public class DefaultKeyVaultSecretManager : IKeyVaultSecretManager
    {
        public bool ShouldLoad(SecretProperties secret) => true;

        public string GetKey(KeyVaultSecret secret)
            => secret?.Name?.Replace("--", ConfigurationPath.KeyDelimiter, System.StringComparison.OrdinalIgnoreCase);
    }
}
