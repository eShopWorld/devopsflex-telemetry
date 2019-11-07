using System.Collections.Generic;
using Autofac;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Registers general Application Inisghts telemetry components.
    /// </summary>
    public class TelemetryModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<IBigBrother>(c =>
            {
                var telemetryClient = c.Resolve<TelemetryClient>();
                var telemetrySettings = c.Resolve<TelemetrySettings>();
                var bb = new BigBrother(telemetryClient, telemetrySettings.InternalKey);

                var bigBrotherInitializers = c.Resolve<IEnumerable<IBigBrotherInitializer>>();
                foreach (var initializer in bigBrotherInitializers)
                {
                    initializer.Initialize(bb);
                }

                return bb;
            })
            .SingleInstance();

            builder.RegisterInstance(LogicalCallTelemetryInitializer.Instance).As<ITelemetryInitializer>();
            builder.RegisterType<BigBrotherEventsPublisherInitializer>().As<IBigBrotherInitializer>();
        }
    }
}
