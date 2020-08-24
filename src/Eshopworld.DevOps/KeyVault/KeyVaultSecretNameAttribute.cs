using System;

namespace Eshopworld.DevOps.KeyVault
{
    /// <summary>
    /// Key vault secret name attribute
    /// Used for Keyvault secret to property mappings
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class KeyVaultSecretNameAttribute : Attribute
    {
        /// <summary>
        /// Key Vault secret name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Key vault secret name attribute constructor
        /// </summary>
        /// <param name="name">Key Vault secret name</param>
        public KeyVaultSecretNameAttribute(string name)
        {
            Name = name;
        }
    }
}
