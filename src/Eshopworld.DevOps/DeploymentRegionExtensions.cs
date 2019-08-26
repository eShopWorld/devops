using System;

namespace Eshopworld.DevOps
{
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// extension methods for <see cref="Regions"/>
    /// </summary>
    public static class DeploymentRegionExtensions
    {
        /// <summary>
        /// to (long) region string value
        /// </summary>
        /// <param name="it">enum instance</param>
        /// <returns>string value of the region</returns>
        public static string ToRegionName(this DeploymentRegion it)
        {            
            return GetAttributeInstance(it).ToString();
        }

        /// <summary>
        /// to short region string value
        /// </summary>
        /// <param name="it">enum instance</param>
        /// <returns>short string value - code - of the region</returns>
        public static string ToRegionCode(this DeploymentRegion it)
        {         
            return GetAttributeInstance(it).ToShortString();
        }

        private static RegionDescriptorAttribute GetAttributeInstance(DeploymentRegion it)
        {
            if (!(typeof(DeploymentRegion).IsEnumDefined(it)))
                throw new ArgumentException("Unrecognized value");

            FieldInfo fi = it.GetType().GetField(it.ToString());
            return (RegionDescriptorAttribute)fi.GetCustomAttributes(
                    typeof(RegionDescriptorAttribute),
                    false).First();
        }
    }
}
