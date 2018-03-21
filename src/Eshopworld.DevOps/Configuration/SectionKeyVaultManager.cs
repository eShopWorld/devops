using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;

namespace Eshopworld.DevOps.Configuration
{
    /// <inheritdoc />
    /// <summary>
    /// this class plugs in the Configuration Extensions API and loads keys into dedicated naming scope
    /// naming structure of secrets as expected
    /// appName-sectionName-secretName (e.g .fraud_api-database-connString)
    /// </summary>
    public class SectionKeyVaultManager : IKeyVaultSecretManager
    {
        /// <summary>
        /// this character is used to separate individual levels within the secret id
        /// </summary>
        private const char LevelSeparator = '-';

        /// <inheritdoc />
        /// <summary>
        /// derive a key for the secret
        /// </summary>
        /// <param name="secret">secret instance</param>
        /// <returns>key for the configuration vault</returns>
        public string GetKey(SecretBundle secret)
        {
            if (secret?.SecretIdentifier == null || string.IsNullOrWhiteSpace(secret.SecretIdentifier.Name))
                throw new InvalidOperationException("secret or its identifier cannot be null/empty");

            var name = secret.SecretIdentifier.Name;
            //strip down the app name portion of the secret
            var firstColonIndex =name.IndexOf(LevelSeparator);
            if (firstColonIndex == (-1) || (firstColonIndex+1)==name.Length) //if not expected naming structure, use the key as it is           
                return name; 
            else
                return name.Substring(firstColonIndex+1)
                    .Replace(LevelSeparator, ':') ;
        }

        /// <inheritdoc />
        /// <summary>
        /// decide whether to load the secret item or not
        /// for the time being, we load all the secrets from the key vault
        /// </summary>
        /// <param name="secret">secret item to evaluate</param>
        /// <returns>true to force item to be loaded</returns>
        public bool Load(SecretItem secret)
        {
            return true; 
        }
    }
}
