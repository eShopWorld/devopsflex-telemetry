﻿namespace Eshopworld.Telemetry.InternalEvents
{
    using Core;

    /// <summary>
    /// Used internally to track calls to the <see cref="IBigBrother.Flush"/> method and find situations where it
    /// is being abused by a developer causing a performance impact.
    /// </summary>
    internal sealed class FlushEvent : TelemetryEvent
    {
    }
}
