namespace Eshopworld.DevOps
{
    /// <summary>
    /// extension methods for <see cref="T:Regions"/>
    /// </summary>
    public static class RegionsExtensions
    {
        /// <summary>
        /// to (long) region string value
        /// </summary>
        /// <param name="it">enum instance</param>
        /// <returns>string value of the region</returns>
        public static string ToRegionString(this Regions it)
        {
            switch (it)
            {
                case Regions.EastUS:
                    return "East US";
                case Regions.WestEurope:
                    return "West Europe";
                default:
                    return "West Europe";
            }
        }

        /// <summary>
        /// to short region string value
        /// </summary>
        /// <param name="it">enum instance</param>
        /// <returns>short string value - code - of the region</returns>
        public static string ToShortRegionString(this Regions it)
        {
            switch (it)
            {
                case Regions.EastUS:
                    return "EUS";
                case Regions.WestEurope:
                    return "WE";
                default:
                    return "WE";
            }
        }
    }
}
