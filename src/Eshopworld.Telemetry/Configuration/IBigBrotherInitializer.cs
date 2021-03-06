﻿using Autofac;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Configures a BigBrother instance during its initialization.
    /// </summary>
    public interface IBigBrotherInitializer
    {
        /// <summary>
        /// Performs the configuration of the BigBrother instance.
        /// </summary>
        /// <param name="bigBrother">The BigBrother instance.</param>
        /// <param name="componentContext">The component context.</param>
        void Initialize(BigBrother bigBrother, IComponentContext componentContext);
    }
}
