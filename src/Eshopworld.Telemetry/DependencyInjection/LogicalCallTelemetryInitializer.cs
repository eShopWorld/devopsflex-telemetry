using System.Collections.Generic;
using System.Threading;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry.DependencyInjection
{
    public class LogicalCallTelemetryInitializer : ITelemetryInitializer
    {
        public static readonly LogicalCallTelemetryInitializer Instance = new LogicalCallTelemetryInitializer();

        private readonly AsyncLocal<Dictionary<string, string>> _propertyValues = new AsyncLocal<Dictionary<string, string>>();

        private LogicalCallTelemetryInitializer()
        {
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
                return;

            var pr = _propertyValues.Value;
            if (pr is null)
                return;

            var telemetryProperties = (ISupportProperties)telemetry;
            foreach (var kv in pr)
                telemetryProperties.Properties[kv.Key] = kv.Value;
        }

        public void SetProperty(string name, string value)
        {
            var dict = _propertyValues.Value;
            if (dict == null)
            {
                dict = new Dictionary<string, string>();
                _propertyValues.Value = dict;
            }

            dict[name] = value;
        }
    }
}
