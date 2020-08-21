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
        /// Name of property to map to
        /// </summary>
        public string PropertyName { get; }


        /// <summary>
        /// Property mapping constructor
        /// </summary>
        /// <param name="propertyName">Name of property to map to</param>
        /// <param name="secretName">Key Vault secret name</param>
        internal PropertySecretMapping(string propertyName, string secretName)
        {
            SecretName = secretName;
            PropertyName = propertyName;
        }
    }
}
