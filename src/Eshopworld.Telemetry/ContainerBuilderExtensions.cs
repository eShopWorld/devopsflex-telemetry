using System;
using Autofac;
using Eshopworld.Telemetry.Configuration;

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
