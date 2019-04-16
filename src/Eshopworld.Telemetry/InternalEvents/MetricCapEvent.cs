using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eshopworld.Core;

namespace Eshopworld.Telemetry.InternalEvents
{
    public sealed class MetricCapEvent : TelemetryEvent
    {
        internal MetricCapEvent(string metricId)
        {
            MetricId = metricId;
        }

        public string MetricId { get; set; }
    }
}
