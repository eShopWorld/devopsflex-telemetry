using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Eshopworld.Telemetry.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace Eshopworld.Telemetry.Tests.Configuration
{
    public class ConfigureBigBrotherInitializerTests
    {
        [Fact, IsLayer0]
        public void InitializerExecutesCallback()
        {
            var bigBrother = new BigBrother();
            var initializer = new ConfigureBigBrotherInitializer((bb, cc) => bb.KustoDbName = "aab");
            var componentContext = new FakeComponentContext();

            initializer.Initialize(bigBrother, componentContext);

            bigBrother.KustoDbName.Should().Be("aab");
        }

        private class FakeComponentContext : IComponentContext
        {
            public IComponentRegistry ComponentRegistry => throw new System.NotImplementedException();

            public object ResolveComponent(IComponentRegistration registration, IEnumerable<Parameter> parameters) => throw new System.NotImplementedException();
        }
    }
}
