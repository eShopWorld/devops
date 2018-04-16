using System;
using System.Runtime.Serialization;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// AAD specific exception
    /// </summary>
    [Serializable]
    public class AADException :Exception
    {
        /// <summary>
        /// message passing constructor
        /// </summary>
        /// <param name="msg">exception message</param>
        public AADException(string msg):base(msg)
        {            
        }

        protected AADException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
