using Microsoft.ApplicationInsights.Channel;

namespace Eshopworld.Telemetry.Processors
{
    /// <summary>
    /// Defines the filter criteria for telemetry filter processor
    /// </summary>
    public interface ITelemetryFilterCriteria
    {
        /// <summary>
        /// Returns true if want to filter the specified telemetry item
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool ShouldFilter(ITelemetry item);
    }
}
