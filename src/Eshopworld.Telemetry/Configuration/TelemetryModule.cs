using System.Collections.Generic;
using Autofac;
using Eshopworld.Core;
using Eshopworld.Telemetry.Initializers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Registers general Application Insights telemetry components.
    /// </summary>
    public class TelemetryModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<IBigBrother>(c =>
            {
                var telemetryClient = c.Resolve<TelemetryClient>();
                var telemetrySettings = c.Resolve<TelemetrySettings>();
                var bb = new BigBrother(telemetryClient, telemetrySettings.InstrumentationKey, telemetrySettings.InternalKey);

                var bigBrotherInitializers = c.Resolve<IEnumerable<IBigBrotherInitializer>>();
                foreach (var initializer in bigBrotherInitializers)
                {
                    initializer.Initialize(bb, c);
                }

                return bb;
            })
            .SingleInstance();

            builder.RegisterInstance(LogicalCallTelemetryInitializer.Instance).As<ITelemetryInitializer>();
            builder.RegisterInstance(new EnvironmentDetailsTelemetryInitializer()).As<ITelemetryInitializer>();
            builder.RegisterType<BigBrotherEventsPublisherInitializer>().As<IBigBrotherInitializer>();
        }
    }
}
