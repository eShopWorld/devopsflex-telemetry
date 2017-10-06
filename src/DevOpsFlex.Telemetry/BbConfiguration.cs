namespace DevOpsFlex.Telemetry
{
    using System.Reactive.Linq;
    using Core;

    /// <summary>
    /// Fluent API extensions for configuring <see cref="BigBrother"/>.
    /// By design the extension methods aren't against <see cref="IBigBrother"/>, because they are meant to be used once, during DI bootstrapping.
    /// </summary>
    public static class BbConfiguration
    {
        /// <summary>
        /// Sets up telemetry to use ETW (Event Tracing for Windows) <see cref="System.Diagnostics.Tracing.EventSource"/>.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <returns></returns>
        public static IConfigureSources UseEventSourceSink(this BigBrother bb)
        {
            return new EventSourceSink(bb);
        }

        /// <summary>
        /// Sets up the previous sink to sink all <see cref="BbExceptionEvent"/> events.
        /// </summary>
        /// <param name="source">The previous configuration source.</param>
        public static void ForExceptions(this IConfigureSources source)
        {
            switch (source)
            {
                case EventSourceSink eSink:
                    var bb = eSink.Bb;

                    BigBrother.ExceptionSinkSubscription?.Dispose();
                    BigBrother.ExceptionSinkSubscription = bb.TelemetryStream.OfType<BbExceptionEvent>().Subscribe(BigBrother.ExceptionStream);

                    break;

                case null:
                    break;
            }
        }
    }

    /// <summary>
    /// Fluent implementation of <see cref="IConfigureSources"/> used to pass on a configuration premise -> <see cref="BigBrother"/>.
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
    /// Fluent interface to pass on a configuration premise.
    /// </summary>
    public interface IConfigureSources
    {
    }
}
