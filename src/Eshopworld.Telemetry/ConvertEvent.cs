namespace Eshopworld.Telemetry
{
    using System;
    using System.Diagnostics;
    using Core;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Handles conversion between <see cref="BaseDomainEvent"/> and <see cref="ITelemetry"/> Application Insights events.
    /// </summary>
    /// <typeparam name="TFrom">The type of the <see cref="BaseDomainEvent"/> we are converting from.</typeparam>
    /// <typeparam name="TTo">The type of the <see cref="ITelemetry"/> event we are converting to.</typeparam>
    public class ConvertEvent<TFrom, TTo>
        where TTo : ITelemetry, ISupportProperties, ISupportMetrics, new()
        where TFrom : BaseDomainEvent
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
        /// <param name="event">The <see cref="BaseDomainEvent"/> that we want to convert from.</param>
        public ConvertEvent([NotNull]TFrom @event)
        {
            // mapping checks, blow up on wrong usage
            if(typeof(TFrom) == typeof(DomainEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(DomainEvent).FullName}");

            if (typeof(TFrom) == typeof(TimedDomainEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(TimedDomainEvent).FullName}");

            if (typeof(TFrom) == typeof(AnonymousDomainEvent) && typeof(TTo) != typeof(EventTelemetry))
                throw new InvalidOperationException($"You can only convert to {typeof(EventTelemetry).FullName} from {typeof(AnonymousDomainEvent).FullName}");

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
            if (Event is DomainEvent bbTelemetryEvent && resultEvent is EventTelemetry telemetry)
            {
                if (Event is AnonymousDomainEvent anonymousEvent)
                    telemetry.Name = anonymousEvent.CallerMemberName;
                else
                    telemetry.Name = bbTelemetryEvent.GetType().Name;
            }

            switch (Event)
            {
                case ExceptionEvent bbEvent:
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
                case TimedDomainEvent bbEvent:
                    if (resultEvent is EventTelemetry timedTelemetry)
                    {
                        timedTelemetry.Metrics[$"{bbEvent.GetType().Name}.{nameof(TimedDomainEvent.ProcessingTime)}"] = bbEvent.ProcessingTime.TotalSeconds;
                    }

                    break;
            }
        }
    }
}
