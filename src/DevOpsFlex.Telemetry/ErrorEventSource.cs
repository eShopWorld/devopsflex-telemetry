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
    [EventSource(Name = EventSourceName)]
    internal sealed class ErrorEventSource : EventSource
    {
        internal const string EventSourceName = "DevOpsFlex-Telemetry-ErrorEvents";

        public class Keywords
        {
            public const EventKeywords Exception = (EventKeywords) (1 << 0);
        }

        public class Tasks
        {
            public const EventTask BbExceptionEvent = (EventTask) (1 << 0);
            public const EventTask Exception =        (EventTask) (1 << 1);
        }

        public static ErrorEventSource Log { get; } = new ErrorEventSource();

        [NonEvent]
        public void Error(Exception ex)
        {
            ExceptionError(ex.Message, ex.StackTrace);
        }

        [NonEvent]
        public void Error(BbExceptionEvent exEvent)
        {
            BbEventError(exEvent.Exception.Message, exEvent.Exception.StackTrace, JsonConvert.SerializeObject(exEvent));
        }

        [Event(
            1,
            Task = Tasks.Exception,
            Keywords = Keywords.Exception,
            Level = EventLevel.Error,
            Channel = EventChannel.Operational
        )]
        public void ExceptionError(string message, string stackTrace)
        {
            WriteEvent(1, message, stackTrace);
        }

        [Event(
            2,
            Task = Tasks.BbExceptionEvent,
            Keywords = Keywords.Exception,
            Level = EventLevel.Error,
            Channel = EventChannel.Operational
        )]
        public void BbEventError(string message, string stackTrace, string eventPayload)
        {
            WriteEvent(2,  message, stackTrace, eventPayload);
        }
    }
}
