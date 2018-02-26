namespace Eshopworld.Telemetry
{
    using DevOpsFlex.Core;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Contains extensions to top level Application Insights constructs.
    /// </summary>
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Populates the Id and CorrelationVector on any <see cref="ITelemetry"/> object with
        /// what it can find on the base <see cref="BbTelemetryEvent"/>.
        /// </summary>
        /// <param name="telemetry">The <see cref="ITelemetry"/> object that we want to populate with correlation data.</param>
        /// <param name="event">The base event we are transforming to the AI object.</param>
        public static void SetCorrelation([NotNull]this ITelemetry telemetry, [NotNull]BbTelemetryEvent @event)
        {
            var vector = @event.CorrelationVector;
            if (vector == null) return;

            telemetry.Context.Operation.CorrelationVector = vector;
            telemetry.Context.Operation.Id = vector;
        }
    }
}
