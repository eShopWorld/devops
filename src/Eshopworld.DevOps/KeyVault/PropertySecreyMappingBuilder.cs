using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Eshopworld.DevOps.KeyVault
{
    public class PropertyMappingBuilder<T>
        where T : class
    {
        public List<PropertySecretMapping> Mappings { get; } = new List<PropertySecretMapping>();

        public PropertyMappingBuilder<T> AddMapping(Expression<Func<T, string>> propertySelector, string secretName)
        {
            var memberExpression = propertySelector.Body as MemberExpression;

            var propertyName = memberExpression?.Member.Name;

            Mappings.Add(new PropertySecretMapping(propertyName, secretName));

            return this;
        }
    }
}
