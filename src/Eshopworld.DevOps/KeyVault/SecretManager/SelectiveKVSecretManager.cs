using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Azure.KeyVault.Models;
using System.Collections.Generic;
using System.IO;

namespace Eshopworld.DevOps.KeyVault.SecretManager
{
    public class SelectiveKVSecretManager : KeyVaultSecretManager
    {
        private readonly Dictionary<string, string> _keys;

        public SelectiveKVSecretManager(Dictionary<string, string> keys)
        {
            _keys = keys;
        }

        public string GetKey(SecretBundle secret)
        {
            if (_keys.ContainsKey(secret?.SecretIdentifier.Name))
                return _keys[secret?.SecretIdentifier.Name];

            throw new InvalidDataException(secret?.SecretIdentifier.Name);
        }

        public bool Load(SecretItem secret)
        {
            return _keys.ContainsKey(secret?.Identifier?.Name);
        }
    }
}