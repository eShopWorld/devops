using System;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// AAD specific exception
    /// </summary>
    public class AADException :Exception
    {
        /// <summary>
        /// message passing constructor
        /// </summary>
        /// <param name="msg">exception message</param>
        public AADException(string msg):base(msg)
        {            
        }
    }
}
