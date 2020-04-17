using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Eshopworld.Telemetry.Processors
{
    /// <summary>
    /// Defines a telemetry filter criteria to filter all telemetry items related to
    /// 'GET' methods of the controller named 'Probe' 
    /// </summary>
    public class SuccessfulProbeFilterCriteria : ITelemetryFilterCriteria
    {
        /// <inheritdoc />
        public bool ShouldFilter(ITelemetry item)
        {
            var request = item as RequestTelemetry;

            if (request == null) return false;

            return
                request.ResponseCode == "200" && (
                    request.Name.StartsWith("GET Probe/", StringComparison.OrdinalIgnoreCase) ||
                    request.Name.StartsWith("HEAD Probe/", StringComparison.OrdinalIgnoreCase)
                );
        }
    }
}
