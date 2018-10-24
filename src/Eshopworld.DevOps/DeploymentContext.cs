using System.Collections.Generic;
using Eshopworld.Core;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// An implementation of deployment context interface <see cref="IDeploymentContext"/>
    /// </summary>
    public class DeploymentContext : IDeploymentContext
    {
        /// <inheritdoc/>
        public IEnumerable<string> PreferredRegions { get; internal set; }
    }
}
