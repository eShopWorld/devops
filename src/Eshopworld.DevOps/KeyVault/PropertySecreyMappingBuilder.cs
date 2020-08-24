using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Eshopworld.DevOps.KeyVault
{
    public class PropertyMappingBuilder<T>
        where T : class
    {
        internal IEnumerable<PropertySecretMapping> Mappings => _mappings;

        private readonly List<PropertySecretMapping> _mappings = new List<PropertySecretMapping>();

        public PropertyMappingBuilder<T> AddMapping(Expression<Func<T, string>> propertySelector, string secretName)
        {
            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            if (string.IsNullOrWhiteSpace(secretName))
                throw new ArgumentNullException(nameof(secretName));

            var memberExpression = propertySelector.Body as MemberExpression;
            var propertyInfo = memberExpression?.Member as PropertyInfo;

            _mappings.Add(new PropertySecretMapping(propertyInfo, secretName));

            return this;
        }
    }
}
