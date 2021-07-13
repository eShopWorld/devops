using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using System.Collections.Generic;
using System.IO;

namespace Eshopworld.DevOps.KeyVault.SecretManager
{
    public class SelectiveKVSecretManager : KeyVaultSecretManager
    {
        private readonly IDictionary<string, string> _keys;

        public SelectiveKVSecretManager(IDictionary<string, string> keys)
        {
            _keys = keys;
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            if (_keys.ContainsKey(secret?.Name))
                return _keys[secret?.Name];

            throw new InvalidDataException(secret?.Name);
        }

        public override bool Load(SecretProperties secret)
        {
            return _keys.ContainsKey(secret?.Name);
        }
    }
}