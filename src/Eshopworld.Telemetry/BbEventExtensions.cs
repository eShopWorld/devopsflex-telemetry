namespace Eshopworld.Telemetry
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
        /// This exists to make the class testable and to allow control over the "Now" during a test.
        /// </summary>
        internal static Func<DateTime> Now = () => DateTime.Now;

        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <param name="event">The event we want to convert to an AI event.</param>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull]
        internal static EventTelemetry ToEventTelemetry([NotNull]this BbTelemetryEvent @event)
        {
            try
            {
                var tEvent = new EventTelemetry
                {
                    Name = @event.GetType().Name,
                    Timestamp = Now()
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
                    Debugger.Break();
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
        internal static ExceptionTelemetry ToExceptionTelemetry([NotNull]this BbExceptionEvent @event)
        {
            try
            {
                if (@event.Exception == null)
                {
                    throw new InvalidOperationException($"Attempt to publish an Exception Event without an exception for type {@event.GetType().FullName}");
                }

                var tEvent = new ExceptionTelemetry
                {
                    Message = @event.Exception.Message,
                    Exception = @event.Exception,
                    Timestamp = Now()
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
                    Debugger.Break();
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
        internal static EventTelemetry ToTimedTelemetry([NotNull]this BbTimedEvent @event)
        {
            try
            {
                var tEvent = new EventTelemetry
                {
                    Name = @event.GetType().Name,
                    Timestamp = Now()
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
                    Debugger.Break();
                    throw;
                }
#endif
                BigBrother.PublishError(ex);
                return null;
            }
        }
    }
}
