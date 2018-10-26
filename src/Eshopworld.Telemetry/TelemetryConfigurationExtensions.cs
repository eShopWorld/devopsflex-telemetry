namespace Eshopworld.Telemetry
{
    using System;
    using System.Reactive.Linq;
    using Core;

    /// <summary>
    /// Fluent API extensions for configuring <see cref="BigBrother"/>.
    /// By design the extension methods aren't against <see cref="IBigBrother"/>, because they are meant to be used once, during DI bootstrapping.
    /// </summary>
    public static class TelemetryConfigurationExtensions
    {
        /// <summary>
        /// Sets up telemetry to use ETW (Event Tracing for Windows) <see cref="System.Diagnostics.Tracing.EventSource"/>.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <returns></returns>
        public static IConfigureSources UseEventSourceSink(this IBigBrother bb)
        {
            return new EventSourceSink((BigBrother)bb);
        }

        /// <summary>
        /// Sets up telemetry to use <see cref="System.Diagnostics.Trace"/> to sink events.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <returns></returns>
        public static IConfigureSources UseTraceSink(this IBigBrother bb)
        {
            return new TraceSink((BigBrother)bb);
        }

        /// <summary>
        /// Sets up the previous sink to sink all <see cref="ExceptionEvent"/> events.
        /// </summary>
        /// <param name="source">The previous configuration source.</param>
        public static void ForExceptions(this IConfigureSources source)
        {
            BigBrother bb;

            switch (source)
            {
                case EventSourceSink eSink:
                    bb = eSink.Bb;

                    bb.EventSourceSinkSubscription?.Dispose();
                    bb.EventSourceSinkSubscription = bb.TelemetryStream.OfType<ExceptionEvent>().Subscribe(BigBrother.SinkToEventSource);

                    break;

                case TraceSink tSink:
                    bb = tSink.Bb;

                    bb.TraceSinkSubscription?.Dispose();
                    bb.TraceSinkSubscription = bb.TelemetryStream.OfType<ExceptionEvent>().Subscribe(BigBrother.SinkToTrace);

                    break;

                case null:
                    break;
            }
        }

    }

    /// <summary>
    /// Fluent implementation of <see cref="IConfigureSources"/> used to pass on a configuration premise for event sources -> <see cref="BigBrother"/>.
    /// </summary>
    public class EventSourceSink : IConfigureSources
    {
        internal EventSourceSink(BigBrother bb)
        {
            Bb = bb;
        }

        internal BigBrother Bb { get; }
    }

    /// <summary>
    /// Fluent implementation of <see cref="IConfigureSources"/> used to pass on a configuration premise for traces -> <see cref="BigBrother"/>.
    /// </summary>
    public class TraceSink : IConfigureSources
    {
        internal TraceSink(BigBrother bb)
        {
            Bb = bb;
        }

        internal BigBrother Bb { get; }
    }

    /// <summary>
    /// Fluent interface to pass on a source configuration premise.
    /// </summary>
    public interface IConfigureSources
    {
    }
}
