namespace Esw.Telemetry.Common
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;

    public class BbExceptionEvent : BbEvent
    {
        public Exception Exception { get; set; }

        public virtual ExceptionTelemetry ToTelemetry()
        {
            return new ExceptionTelemetry
            {
                HandledAt = ExceptionHandledAt.Platform,
                Message = Exception.Message,
                Exception = Exception,
                SeverityLevel = SeverityLevel.Warning
            };
        }
    }

    public static class BbExceptionEventExtensions
    {
        public static BbExceptionEvent ToBbEvent(this Exception exception)
        {
            return new BbExceptionEvent
            {
                Exception = exception
            };
        }
    }
}