using System;
using System.Reflection;

namespace Eshopworld.DevOps.KeyVault
{
    /// <summary>
    /// Mapping data for mapping Key Vault secrets to properties
    /// </summary>
    public class PropertySecretMapping
    {
        /// <summary>
        /// Key Vault secret name
        /// </summary>
        public string SecretName { get; }
        
        /// <summary>
        /// Property to map to
        /// </summary>
        public PropertyInfo PropertyInfo { get; }


        /// <summary>
        /// Property mapping constructor
        /// </summary>
        /// <param name="propertyInfo">Property to map to</param>
        /// <param name="secretName">Key Vault secret name</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal PropertySecretMapping(PropertyInfo propertyInfo, string secretName)
        {
            SecretName = secretName;
            PropertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        }
    }
}
