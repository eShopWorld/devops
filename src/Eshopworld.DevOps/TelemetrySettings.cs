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
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// Gets and sets the internal instrumentation key.
        /// </summary>
        public string InternalKey { get; set; }
    }
}