namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Reactive.Linq;
    using Core;

    public static class BbConfiguration
    {
        public static IConfigureSources UseEventSourceSink(this BigBrother bb)
        {
            return new EventSourceSink(bb);
        }

        public static void ForExceptions(this IConfigureSources source)
        {
            switch (source)
            {
                case EventSourceSink eSink:
                    var bb = eSink.Bb;

                    bb.TelemetrySubscriptions.AddSubscription(
                        typeof(EventSourceSink),
                        bb.TelemetryStream.OfType<BbExceptionEvent>().Subscribe(e => BigBrother.ExceptionStream.OnNext(e.Exception)));

                    break;

                case null:
                    break;
            }
        }
    }

    public class EventSourceSink : IConfigureSources
    {
        internal EventSourceSink(BigBrother bb)
        {
            Bb = bb;
        }

        internal BigBrother Bb { get; }
    }

    public interface IConfigureSources
    {
    }
}
