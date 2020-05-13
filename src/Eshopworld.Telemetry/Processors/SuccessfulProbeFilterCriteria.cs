using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Eshopworld.Telemetry.Processors
{
    /// <summary>
    /// Defines a telemetry filter criteria
    ///     -To filter all telemetry items related to 'GET' and 'HEAD' methods of the controller named 'Probe' with OK response
    ///     -To filter the telemetry items for the specified health check path with OK response
    /// </summary>
    public class SuccessfulProbeFilterCriteria : ITelemetryFilterCriteria
    {
        private readonly string[] _methodNames = new[] {"GET", "HEAD"};

        private const string DefaultHealthCheckPath = "probe/";

        private readonly List<string> _requestNamesToMatch;

        /// <summary>
        /// Creates an instance of <see cref="SuccessfulProbeFilterCriteria"/>
        /// </summary>
        /// <param name="healthChecksPath"></param>
        public SuccessfulProbeFilterCriteria(string healthChecksPath = null)
        {
            IEnumerable<string> BuildMatches(string path) =>
                _methodNames.Select(name => $"{name} {path}");

            _requestNamesToMatch = new List<string>();

            _requestNamesToMatch.AddRange(BuildMatches(DefaultHealthCheckPath));

            if (!string.IsNullOrWhiteSpace(healthChecksPath))
            {
                _requestNamesToMatch.AddRange(BuildMatches(healthChecksPath));
            }
        }

        /// <inheritdoc />
        public bool ShouldFilter(ITelemetry item)
        {
            if (!(item is RequestTelemetry request)) return false;

            return request.ResponseCode == "200" && IsMatch(request.Name);
        }

        private bool IsMatch(string requestName)
        {
            return _requestNamesToMatch.Any(nameToMatch =>
                requestName.StartsWith(nameToMatch, StringComparison.OrdinalIgnoreCase));
        }
    }
}