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
        private readonly string[] _methodNames =  {"GET", "HEAD"};

        private readonly string[] _defaultProbePaths = { "probe/", "/probe" };

        private readonly List<string> _requestNamesToMatch;

        /// <summary>
        /// Creates an instance of <see cref="SuccessfulProbeFilterCriteria"/>
        /// </summary>
        /// <param name="healthChecksPaths"></param>
        public SuccessfulProbeFilterCriteria(params string[] healthChecksPaths)
        {
            IEnumerable<string> BuildMatches(string[] paths) => _methodNames
                .SelectMany(name => paths.Select(path => (name, path)))
                .Select(t => $"{t.name} {t.path}");

            _requestNamesToMatch = new List<string>();

            _requestNamesToMatch.AddRange(BuildMatches(_defaultProbePaths));

            if (healthChecksPaths!=null && healthChecksPaths.Any())
            {
                _requestNamesToMatch.AddRange(BuildMatches(healthChecksPaths));
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