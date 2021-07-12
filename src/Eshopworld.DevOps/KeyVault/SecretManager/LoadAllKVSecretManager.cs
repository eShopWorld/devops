using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Azure.KeyVault.Models;

namespace Eshopworld.DevOps.KeyVault.SecretManager
{
    public class LoadAllKVSecretManager : KeyVaultSecretManager
    {
        public string GetKey(SecretBundle secret)
        {
            return secret?.SecretIdentifier.Name;
        }

        public bool Load(SecretItem secret)
        {
            return true;
        }
    }
}
