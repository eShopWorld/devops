using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Eshopworld.DevOps.AzureKeyVault
{
    public class AzureKeyVaultConfigurationSource : IConfigurationSource
    {
        public SecretClient Client { get; set; }

        public IKeyVaultSecretManager Manager { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureKeyVaultConfigurationProvider(Client, Manager);
        }
    }
}
