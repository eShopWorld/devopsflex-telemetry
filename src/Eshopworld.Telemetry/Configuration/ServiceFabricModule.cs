using System.Fabric;
using Autofac;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.ApplicationInsights.ServiceFabric.Module;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Registers Service Fabric components of telemetry pipeline.
    /// </summary>
    public class ServiceFabricModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(x =>
            {
                var serviceContext = x.ResolveOptional<ServiceContext>();
                return FabricTelemetryInitializerExtension.CreateFabricTelemetryInitializer(serviceContext);
            }).As<ITelemetryInitializer>();
            builder.Register(c => new ServiceRemotingRequestTrackingTelemetryModule { SetComponentCorrelationHttpHeaders = true }).As<ITelemetryModule>();
            builder.RegisterType<ServiceRemotingDependencyTrackingTelemetryModule>().As<ITelemetryModule>();
        }
    }
}
