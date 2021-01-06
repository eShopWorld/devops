using Eshopworld.DevOps.KeyVault;

namespace Eshopworld.DevOps
{
    /// <summary>
    /// Contains settings related to Telemetry.
    /// </summary>
    public class TelemetrySettings
    {
        /// <summary>
        /// Gets and sets the main telemetry instrumentation key.
        /// </summary>
        [KeyVaultSecretName("cm--ai-telemetry--instrumentation")]
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Gets and sets the internal instrumentation key.
        /// </summary>
        [KeyVaultSecretName("cm--ai-telemetry--internal")]
        public string InternalKey { get; set; }
    }
}