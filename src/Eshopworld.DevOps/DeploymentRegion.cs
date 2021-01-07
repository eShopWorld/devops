using System.Runtime.CompilerServices;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// regions that evolution recognizes
    /// </summary>
    public enum DeploymentRegion
    {
        [SpecialName]
        None,
        [RegionDescriptor("West Europe", "WE")]
        WestEurope,
        // ReSharper disable once InconsistentNaming
        [RegionDescriptor("East US", "EUS")]
        EastUS,
        [RegionDescriptor("Southeast Asia", "SA")]
        SoutheastAsia
    }
}
