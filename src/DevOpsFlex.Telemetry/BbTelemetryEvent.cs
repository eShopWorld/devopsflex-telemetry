namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.DataContracts;
    using Newtonsoft.Json;

    /// <summary>
    /// The base class from all BigBrother telemetry based events that are going to be
    /// tracked by AI as <see cref="EventTelemetry"/> events.
    /// </summary>
    public class BbTelemetryEvent : BbEvent
    {
        /// <summary>
        /// Converts this event into an Application Insights <see cref="EventTelemetry"/> event ready to be tracked by
        /// the AI client.
        /// </summary>
        /// <returns>The converted <see cref="EventTelemetry"/> event.</returns>
        [CanBeNull] public virtual EventTelemetry ToTelemetry()
        {
            try
            {
                var properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    JsonConvert.SerializeObject(this));

                var tEvent = new EventTelemetry
                {
                    Name = GetType().Name,
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
}
