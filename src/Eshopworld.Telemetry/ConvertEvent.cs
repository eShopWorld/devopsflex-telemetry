namespace Eshopworld.Telemetry
{
    using System;
    using System.Diagnostics;
    using Core;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Handles conversion between <see cref="BbEvent"/> and <see cref="ITelemetry"/> Application Insights events.
    /// </summary>
    /// <typeparam name="TFrom">The type of the <see cref="BbEvent"/> we are converting from.</typeparam>
    /// <typeparam name="TTo">The type of the <see cref="ITelemetry"/> event we are converting to.</typeparam>
    public class ConvertEvent<TFrom, TTo>
        where TTo : ITelemetry, ISupportProperties, ISupportMetrics, new()
        where TFrom : BbEvent
    {
        internal readonly TFrom Event;

        /// <summary>
        /// This exists to make the class testable and to allow control over the "Now" during a test.
        /// </summary>
        [NotNull]
        internal Func<DateTime> Now = () => DateTime.Now;

        /// <summary>
        /// Initialilzes a new instance of <see cref="ConvertEvent{TFrom,TTo}"/>.
        /// </summary>
        /// <param name="event">The <see cref="BbEvent"/> that we want to convert from.</param>
        public ConvertEvent([NotNull]TFrom @event)
        {
            // mapping checks, blow up on wrong usage
            if(typeof(TFrom) == typeof(BbTelemetryEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(BbTelemetryEvent).FullName}");

            if (typeof(TFrom) == typeof(BbTimedEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(BbTimedEvent).FullName}");

            if (typeof(TFrom) == typeof(BbAnonymousEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(BbAnonymousEvent).FullName}");

            if (typeof(TFrom) == typeof(BbExceptionEvent) && typeof(TTo) != typeof(ExceptionTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(ExceptionTelemetry).FullName} from {typeof(BbExceptionEvent).FullName}");

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
            if (Event is BbTelemetryEvent bbTelemetryEvent && resultEvent is EventTelemetry telemetry)
            {
                if (Event is BbAnonymousEvent anonymousEvent)
                    telemetry.Name = anonymousEvent.CallerMemberName;
                else
                    telemetry.Name = bbTelemetryEvent.GetType().Name;
            }

            switch (Event)
            {
                case BbExceptionEvent bbEvent:
                    if (resultEvent is ExceptionTelemetry exceptionTelemetry)
                    {
                        if (bbEvent.Exception == null)
                        {
                            throw new InvalidOperationException($"Attempt to publish an Exception Event without an exception for type {exceptionTelemetry.GetType().FullName}");
                        }

                        exceptionTelemetry.Message = bbEvent.Exception.Message;
                        exceptionTelemetry.Exception = bbEvent.Exception;
                    }

                    break;
                case BbTimedEvent bbEvent:
                    if (resultEvent is EventTelemetry timedTelemetry)
                    {
                        timedTelemetry.Metrics[$"{bbEvent.GetType().Name}.{nameof(BbTimedEvent.ProcessingTime)}"] = bbEvent.ProcessingTime.TotalSeconds;
                    }

                    break;
            }
        }
    }
}
