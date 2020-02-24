using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

namespace Eshopworld.Telemetry.Initializers
{
    /// <summary>Intercepts AI events, and adds additional environment values for tracing purposes</summary>
    /// <example>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     services.AddSingleton&lt;ITelemetryInitializer, Eshopworld.Telemetry.Initializers.EnvironmentDetailsTelemetryInitializer&gt;();
    ///     // additional
    /// }
    /// </example>
    public class EnvironmentDetailsTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>Names of environment variables which will be inspected to append to AI events</summary>
        public static readonly string[] RequiredEnvironmentVariables = { "applicationName", "domain", "tenant", "region", "environment" };

        private readonly (string name, string value)[] EnvironmentVariablesValues;

        /// <summary>Obtains environment variables from <see cref="Environment.GetEnvironmentVariable(string)"/></summary>
        public EnvironmentDetailsTelemetryInitializer()
        {
            EnvironmentVariablesValues = RequiredEnvironmentVariables.Select(n => (n, Environment.GetEnvironmentVariable(n))).ToArray();
        }

        /// <summary>Obtains environment variables from <see cref="IConfigurationRoot"/></summary>
        public EnvironmentDetailsTelemetryInitializer([NotNull] IConfigurationRoot configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            EnvironmentVariablesValues = RequiredEnvironmentVariables.Select(n => (n, configuration.GetSection(n).Value)).ToArray();
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null) return;

            Array.ForEach(EnvironmentVariablesValues, t => telemetry.Context.GlobalProperties[$"esw-{t.name}"] = t.value);
        }
    }
}