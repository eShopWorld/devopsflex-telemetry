namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Diagnostics.Tracing;
    using Core;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides the ability to create exception events for event tracing for Windows (ETW).
    /// </summary>
    /// <remarks>
    /// Used to sink exceptions in <see cref="BigBrother"/> to support Service Fabric deployments.
    /// </remarks>
    [EventSource(Name = "DevOpsFlex-Telemetry-ExceptionEvents")]
    internal class ErrorEventSource : EventSource
    {
        internal class Keywords
        {
            internal const EventKeywords Exception = (EventKeywords) (1 << 0);
        }

        internal class Tasks
        {
            internal const EventTask BbExceptionEvent = (EventTask) (1 << 0);
            internal const EventTask Exception =        (EventTask) (1 << 0);
        }

        internal static ErrorEventSource Log { get; } = new ErrorEventSource();

        /// <summary>
        /// Initializes a new instance of <see cref="ErrorEventSource"/> as an <see cref="EventSourceSettings.EtwSelfDescribingEventFormat"/> <see cref="EventSource"/>.
        /// </summary>
        private ErrorEventSource()
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        internal void Error(Exception ex)
        {
            Error(ex.Message, ex.StackTrace);
        }

        internal void Error(BbExceptionEvent exEvent)
        {
            Error(exEvent.Exception.Message, exEvent.Exception.StackTrace, JsonConvert.SerializeObject(exEvent));
        }

        [Event(
            1,
            Task = Tasks.Exception,
            Keywords = Keywords.Exception,
            Level = EventLevel.Error
        )]
        private void Error(string message, string stackTrace)
        {
            WriteEvent(1, message, stackTrace);
        }

        [Event(
            2,
            Task = Tasks.BbExceptionEvent,
            Keywords = Keywords.Exception,
            Level = EventLevel.Error
            )]
        private void Error(string message, string stackTrace, string eventPayload)
        {
            WriteEvent(1,  message, stackTrace, eventPayload);
        }
    }
}
