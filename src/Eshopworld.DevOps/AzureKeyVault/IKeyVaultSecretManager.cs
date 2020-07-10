using Azure.Security.KeyVault.Secrets;

namespace Eshopworld.DevOps.AzureKeyVault
{
    public interface IKeyVaultSecretManager
    {
        bool ShouldLoad(SecretProperties secret);

        string GetKey(KeyVaultSecret secret);
    }
}
