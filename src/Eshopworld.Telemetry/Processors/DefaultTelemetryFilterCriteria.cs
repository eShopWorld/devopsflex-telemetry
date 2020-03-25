using Microsoft.ApplicationInsights.Channel;

namespace Eshopworld.Telemetry.Processors
{
    /// <summary>
    /// Defines a default telemetry filter criteria
    /// Returns false; not to filter any telemetry item
    /// </summary>
    public class DefaultTelemetryFilterCriteria : ITelemetryFilterCriteria
    {
        /// <inheritdoc />
        public bool ShouldFilter(ITelemetry item)
        {
            return false;
        }
    }
}
