using System;
using Eshopworld.Core;

namespace Eshopworld.Telemetry.InternalEvents
{
    internal class SimplifiedExceptionEvent : ExceptionEvent
    {
        public SimplifiedExceptionEvent(Exception exception)
            : base(exception)
        {
        }

        public override bool SimplifyStackTrace => false; // Simplification failed so do not use it to simplify this stack trace
    }
}
