using Eshopworld.Core;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// default implementation of <see cref="IAADContext"/>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class AADContext : IAADContext
    {
        public string AuthFilePath { get; internal set; }
        public string TenantId { get; internal set; }
        public string SubscriptionId { get; internal set; }
        public string ClientId { get; internal set; }
        public string ClientSecret { get; internal set; }
    }
}
