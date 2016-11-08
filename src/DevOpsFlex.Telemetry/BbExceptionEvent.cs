namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Esw.Telemetry.Common;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.DataContracts;
    using Newtonsoft.Json;

    /// <summary>
    /// The base class from all BigBrother <see cref="Exception"/> based events that are going to be
    /// tracked by AI as <see cref="ExceptionTelemetry"/> events.
    /// </summary>
    public class BbExceptionEvent : BbEvent
    {
        /// <summary>
        /// Gets and sets the raw <see cref="Exception"/> that is associated with this event.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Quick dirty way to prevent JsonConvert from attempting to serialize the exception.
        /// This could be improved by a generic IContractResolver, but we don't care singe we're not planning on keeping JsonConvert.
        /// </summary>
        /// <returns>false.</returns>
        public bool ShouldSerializeException() { return false; }

        /// <summary>
        /// Converts this event into an Application Insights <see cref="ExceptionTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <returns>The converted <see cref="ExceptionTelemetry"/> event.</returns>
        [CanBeNull] public virtual ExceptionTelemetry ToTelemetry()
        {
            try
            {
                var properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(this));

                var tEvent = new ExceptionTelemetry
                {
                    Message = Exception.Message,
                    Exception = Exception,
                    Timestamp = DateTimeOffset.Now
                };

                foreach (var key in properties.Keys)
                {
                    tEvent.Properties.Add(key, properties[key]);
                }

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
    }

    /// <summary>
    /// Containst conversion extensions to <see cref="Exception"/> to produce events of, or based of <see cref="BbExceptionEvent"/>.
    /// </summary>
    public static class BbExceptionEventExtensions
    {
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