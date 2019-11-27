using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Integration.ServiceFabric;
using Eshopworld.Telemetry.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry
{
    /// <summary>
    /// Defines extension methods helping with configuring BigBrother instance.
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Registers a BigBrother configuration callback.
        /// </summary>
        /// <param name="containerBuilder">The container builder used to build BigBrother.</param>
        /// <param name="configure">The BigBrother configuration callback.</param>
        /// <returns>The container builder.</returns>
        public static ContainerBuilder ConfigureBigBrother(this ContainerBuilder containerBuilder, Action<BigBrother> configure)
        {
            containerBuilder.RegisterInstance<IBigBrotherInitializer>(new ConfigureBigBrotherInitializer((bigBrother, componentContext) => configure(bigBrother)));
            return containerBuilder;
        }

        /// <summary>
        /// Registers a BigBrother configuration callback.
        /// </summary>
        /// <param name="containerBuilder">The container builder used to build BigBrother.</param>
        /// <param name="configure">The BigBrother configuration callback.</param>
        /// <returns>The container builder.</returns>
        public static ContainerBuilder ConfigureBigBrother(this ContainerBuilder containerBuilder, Action<BigBrother, IComponentContext> configure)
        {
            containerBuilder.RegisterInstance<IBigBrotherInitializer>(new ConfigureBigBrotherInitializer((bigBrother, componentContext) => configure(bigBrother, componentContext)));
            return containerBuilder;
        }

        /// <summary>
        /// Configures telemetry initializers for statefull services.
        /// Warning: It must not be used in Asp.Net Core projects.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The container builder.</returns>
        public static ContainerBuilder AddStatefullServiceTelemetry(this ContainerBuilder builder)
        {
            builder.RegisterType<OperationCorrelationTelemetryInitializer>().As<ITelemetryInitializer>();
            builder.RegisterType<HttpDependenciesParsingTelemetryInitializer>().As<ITelemetryInitializer>();
            builder.Register(c=> {             
                var module = new DependencyTrackingTelemetryModule();
                module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
                module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

                return module;
            }).As<ITelemetryModule>();

            builder.RegisterType<TelemetryClient>().SingleInstance();

            builder.RegisterServiceFabricSupport();

            builder.Register(c =>
            {
                var telemetrySettings = c.Resolve<TelemetrySettings>();
                var configuration = new TelemetryConfiguration(telemetrySettings.InstrumentationKey);
                foreach (var initializer in c.Resolve<IEnumerable<ITelemetryInitializer>>())
                {
                    configuration.TelemetryInitializers.Add(initializer);
                }

                foreach (var module in c.Resolve<IEnumerable<ITelemetryModule>>())
                {
                    module.Initialize(configuration);
                }

                return configuration;
            });

            return builder;
        }

        public static ContainerBuilder ConfigureTelemetryKeys(this ContainerBuilder builder, string instrumentationKey, string internalKey)
        {
            builder.RegisterInstance(new TelemetrySettings
            {
                InstrumentationKey = instrumentationKey,
                InternalKey = internalKey,
            });

            return builder;
        }
    }
}
