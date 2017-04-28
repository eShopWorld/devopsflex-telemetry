namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Diagnostics;
    using Core;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Contains extension methods to the base type <see cref="BbEvent"/> and any derived event class.
    /// </summary>
    public static class BbEventExtensions
    {
        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <param name="event">The event we want to convert to an AI event.</param>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull]
        internal static EventTelemetry ToTelemetry([NotNull]this BbTelemetryEvent @event)
        {
            try
            {
                var tEvent = new EventTelemetry
                {
                    Name = @event.GetType().Name,
                    Timestamp = DateTimeOffset.Now
                };

                tEvent.SetCorrelation(@event);
                @event.CopyPropertiesInto(tEvent.Properties);

                return tEvent;
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    throw;
                }
#endif
                BigBrother.PublishError(ex);
                return null;
            }
        }

        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <param name="event">The event we want to convert to an AI event.</param>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull]
        internal static ExceptionTelemetry ToTelemetry([NotNull]this BbExceptionEvent @event)
        {
            try
            {
                var tEvent = new ExceptionTelemetry
                {
                    Message = @event.Exception.Message,
                    Exception = @event.Exception,
                    Timestamp = DateTimeOffset.Now
                };

                tEvent.SetCorrelation(@event);
                @event.CopyPropertiesInto(tEvent.Properties);

                return tEvent;
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    throw;
                }
#endif
                BigBrother.PublishError(ex);
                return null;
            }
        }

        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <param name="event">The event we want to convert to an AI event.</param>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull]
        internal static EventTelemetry ToTelemetry([NotNull]this BbTimedEvent @event)
        {
            try
            {
                var tEvent = new EventTelemetry
                {
                    Name = @event.GetType().Name,
                    Timestamp = DateTimeOffset.Now,
                };

                tEvent.Metrics[nameof(BbTimedEvent.ProcessingTime)] = @event.ProcessingTime.TotalSeconds;

                tEvent.SetCorrelation(@event);
                @event.CopyPropertiesInto(tEvent.Properties);

                return tEvent;
            }
            catch (Exception ex)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    throw;
                }
#endif
                BigBrother.PublishError(ex);
                return null;
            }
        }

        /// <summary>
        /// Converts a generic <see cref="Exception"/> into a <see cref="BbExceptionEvent"/>.
        /// </summary>
        /// <param name="exception">The original <see cref="Exception"/>.</param>
        /// <returns>The converted <see cref="BbExceptionEvent"/>.</returns>
        public static BbExceptionEvent ToBbEvent(this Exception exception)
        {
            return ToBbEvent<BbExceptionEvent>(exception);
        }

        /// <summary>
        /// Converts a generic <see cref="Exception"/> into any class that inherits from <see cref="BbExceptionEvent"/>.
        /// </summary>
        /// <typeparam name="T">The type of the specific <see cref="BbExceptionEvent"/> super class.</typeparam>
        /// <param name="exception">The original <see cref="Exception"/>.</param>
        /// <returns>The converted super class of <see cref="BbExceptionEvent"/>.</returns>
        public static T ToBbEvent<T>(this Exception exception)
            where T : BbExceptionEvent, new()
        {
            return new T
            {
                Exception = exception
            };
        }
    }
}
