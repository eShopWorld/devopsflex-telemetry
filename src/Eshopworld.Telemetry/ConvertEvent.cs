namespace Eshopworld.Telemetry
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Core;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Handles conversion between <see cref="BaseEvent"/> and <see cref="ITelemetry"/> Application Insights events.
    /// </summary>
    /// <typeparam name="TFrom">The type of the <see cref="BaseEvent"/> we are converting from.</typeparam>
    /// <typeparam name="TTo">The type of the <see cref="ITelemetry"/> event we are converting to.</typeparam>
    public class ConvertEvent<TFrom, TTo>
        where TTo : ITelemetry, ISupportProperties, ISupportMetrics, new()
        where TFrom : BaseEvent
    {
        internal readonly TFrom Event;

        /// <summary>
        /// This exists to make the class testable and to allow control over the "Now" during a test.
        /// </summary>
        [NotNull]
        internal Func<DateTime> Now = () => DateTime.Now;

        /// <summary>
        /// Initializes a new instance of <see cref="ConvertEvent{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="event">The <see cref="BaseEvent"/> that we want to convert from.</param>
        public ConvertEvent([NotNull]TFrom @event)
        {
            // mapping checks, blow up on wrong usage
            if (typeof(TFrom) == typeof(TelemetryEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(TelemetryEvent).FullName}");

            if (typeof(TFrom) == typeof(TimedTelemetryEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(TimedTelemetryEvent).FullName}");

            if (typeof(TFrom) == typeof(AnonymousTelemetryEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(AnonymousTelemetryEvent).FullName}");

            if (typeof(TFrom) == typeof(ExceptionEvent) && typeof(TTo) != typeof(ExceptionTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(ExceptionTelemetry).FullName} from {typeof(ExceptionEvent).FullName}");

            Event = @event;
        }

        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull]
        public TTo ToTelemetry()
        {
            try
            {
                var resultEvent = new TTo
                {
                    Timestamp = Now()
                };

                // Specific event type handling
                HandleEventTypes(resultEvent);

                Event.CopyPropertiesInto(resultEvent.Properties);

                return resultEvent;
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                    throw;
                }
#endif
                BigBrother.PublishError(ex);
                return default;
            }
        }

        /// <summary>
        /// Handles specific event details relative to the To and From event types.
        /// </summary>
        /// <param name="resultEvent">The To event that needs to be populated with specific details.</param>
        internal void HandleEventTypes(TTo resultEvent)
        {
            if (Event is TelemetryEvent telemetryEvent && resultEvent is EventTelemetry telemetry)
            {
                if (Event is AnonymousTelemetryEvent anonymousEvent)
                    telemetry.Name = anonymousEvent.CallerMemberName;
                else
                    telemetry.Name = telemetryEvent.GetType().Name;
            }

            switch (Event)
            {
                case ExceptionEvent exceptionEvent:
                    if (resultEvent is ExceptionTelemetry exceptionTelemetry)
                    {
                        if (exceptionEvent.Exception == null)
                        {
                            throw new InvalidOperationException($"Attempt to publish an Exception Event without an exception for type {exceptionTelemetry.GetType().FullName}");
                        }

                        exceptionTelemetry.Message = exceptionEvent.Exception.Message;
                        exceptionTelemetry.Exception = exceptionEvent.Exception;

                        if (exceptionEvent.SimplifyStackTrace && StackTraceHelper.IsStackSimplificationAvailable)
                        {
                            try
                            {
                                var stackTrace = StackTraceHelper.SimplifyStackTrace(exceptionTelemetry.Exception);
                                exceptionTelemetry.SetParsedStack(stackTrace.ToArray());
                            }
                            catch (Exception ex)
                            {
                                // Preserve the original stack trace append some info about the problem. Report the problem to the internal stream.
                                exceptionTelemetry.Properties["SimplifyStackTraceFailed"] = ex.Message;
                                BigBrother.InternalStream.OnNext(ex.ToExceptionEvent<SimplifiedExceptionEvent>());
                            }
                        }
                    }

                    break;
                case TimedTelemetryEvent timedTelemetryEvent:
                    if (resultEvent is EventTelemetry timedTelemetry)
                    {
                        timedTelemetry.Metrics[$"{timedTelemetryEvent.GetType().Name}.{nameof(TimedTelemetryEvent.ProcessingTime)}"] = timedTelemetryEvent.ProcessingTime.TotalSeconds;
                    }

                    break;
            }
        }

        internal class SimplifiedExceptionEvent : ExceptionEvent
        {
            public SimplifiedExceptionEvent(Exception exception)
                : base(exception)
            {
            }

            public override bool SimplifyStackTrace => false; // Simplification failed so do not use it to simplify this stack trace
        }
    }
}
