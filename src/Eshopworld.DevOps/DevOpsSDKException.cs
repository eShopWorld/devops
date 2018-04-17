using System;
using System.Runtime.Serialization;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// Devops SDKexception
    /// </summary>
    [Serializable]
    // ReSharper disable once InconsistentNaming
    public class DevOpsSDKException :Exception
    {
        /// <summary>
        /// message passing constructor
        /// </summary>
        /// <param name="msg">exception message</param>
        public DevOpsSDKException(string msg):base(msg)
        {            
        }

        protected DevOpsSDKException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
