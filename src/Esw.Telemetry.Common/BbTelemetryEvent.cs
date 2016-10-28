namespace Esw.Telemetry.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.DataContracts;
    using Newtonsoft.Json;

    public class BbTelemetryEvent : BbEvent
    {
        [CanBeNull] public virtual EventTelemetry ToTelemetry()
        {
            try
            {
                // TODO: This is too slow to be used out of alpha, replace with either emit or T4 gen + Roslyn
                // TODO: When this is removed it also makes the current depedency on newtonsoft.json go away
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
