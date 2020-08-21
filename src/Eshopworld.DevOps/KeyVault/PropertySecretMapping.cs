using System;
using System.Linq.Expressions;

namespace Eshopworld.DevOps.KeyVault
{
    /// <summary>
    /// Mapping data for mapping Key Vault secrets to properties
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertySecretMapping<T>
        where T : class
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
        /// <param name="secretName">Key Vault secret name</param>
        /// <param name="propertySelector">Selector for property to map to</param>
        public PropertySecretMapping(string secretName, Expression<Func<T, string>> propertySelector)
        {
            SecretName = secretName;

            var memberExpression = propertySelector.Body as MemberExpression;

            PropertyName = memberExpression?.Member.Name;
        }

        /// <summary>
        /// Property mapping constructor
        /// </summary>
        /// <param name="secretName">Key Vault secret name</param>
        /// <param name="propertyName">Name of property to map to</param>
        internal PropertySecretMapping(string secretName, string propertyName)
        {
            SecretName = secretName;
            PropertyName = propertyName;
        }
    }
}
