namespace Eshopworld.DevOps
{
    using System;

    /// <summary>
    /// attribute to capture region string name and the code
    /// </summary>
    public class RegionDescriptorAttribute : Attribute
    {
        private readonly string _fullName;
        private readonly string _code;

        public RegionDescriptorAttribute(string fullName, string code)
        {
            _fullName = fullName;
            _code = code;
        }

        public override string ToString()
        {
            return _fullName;
        }

        public string ToShortString()
        {
            return _code;
        }
    }
}
