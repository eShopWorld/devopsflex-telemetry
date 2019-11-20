using System;
using Autofac;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Configures BigBrother using custom configuration callback.
    /// </summary>
    class ConfigureBigBrotherInitializer : IBigBrotherInitializer
    {
        private readonly Action<BigBrother, IComponentContext> _initializer;

        /// <summary>
        /// Creates an instance of the initializer.
        /// </summary>
        /// <param name="initializer">The BigBrother initialization callback.</param>
        public ConfigureBigBrotherInitializer(Action<BigBrother, IComponentContext> initializer)
        {
            _initializer = initializer;
        }

        /// <summary>
        /// Initializes BigBrother
        /// </summary>
        /// <param name="bigBrother">The BigBrother instance to initialize.</param>
        /// <param name="componentContext">The component context.</param>
        public void Initialize(BigBrother bigBrother, IComponentContext componentContext)
        {
            _initializer.Invoke(bigBrother, componentContext);
        }
    }
}
