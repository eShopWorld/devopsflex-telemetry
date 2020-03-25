using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry.Processors
{
    /// <summary>
    /// Filters out health probes. 
    /// Use the ProbePath parameter to customize your probe path (case unsensitive).
    /// If you don't specify a probe path it will default to /Probe.
    /// </summary>
    /// <example>
    /// &lt;TelemetryProcessors&gt;
    /// &lt;!-- Add this to ApplicationInsights.config or leave it blank to default it to /Probe --&gt;
    ///     &lt;Add Type="DevOpsFlex.Telemetry.ProbeTelemetryProcessor, DevOpsFlex.Telemetry"&gt;
    ///         &lt;ProbePath>/Probe&lt;/ProbePath&gt;
    ///     &lt;/Add&gt;
    /// &lt;/TelemetryProcessors&gt;
    /// 
    /// // Alternatively, you can perform initialization in code.
    /// // Use an initialization class (such as Global.asax.cs) to add the processor to the chain.
    /// 
    /// var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
    /// builder.Use((next) => new ProbeTelemetryProcessor(next) { ProbePath = "/CustomProbePath" });
    /// builder.Build();
    /// </example>
    public class TelemetryFilterProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryFilterCriteria filterCriteria;
        private readonly ITelemetryProcessor next;

        /// <summary>
        /// Link processors to each other in a chain.
        /// </summary>
        public TelemetryFilterProcessor(ITelemetryProcessor next, ITelemetryFilterCriteria filterCriteria)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.filterCriteria = filterCriteria ?? throw new ArgumentNullException(nameof(filterCriteria));
        }
        public void Process(ITelemetry item)
        {
            if (!filterCriteria.ShouldFilter(item))
            {
                next.Process(item);
            }
        }
    }

}
