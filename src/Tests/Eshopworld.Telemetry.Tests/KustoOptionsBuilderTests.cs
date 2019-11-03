using Eshopworld.Core;
using Eshopworld.Telemetry.Kusto;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class KustoOptionsBuilderTests
    {
        [Fact, IsUnit]
        public void Test_IsRegistered_ToDirectClient()
        {
            var bb = new BigBrother();

            bb.UseKusto()
              .WithCluster("", "", "", "").RegisterAssembly(GetType().Assembly).WithDirectClient()
              .RegisterType<OptionsTestEvent>().WithDirectClient().Build();

            var builder = new KustoOptionsBuilder();

            builder.RegisterType<OptionsTestEvent>().WithDirectClient();

            builder.RegisteredDirectTypes.Should().Contain(typeof(OptionsTestEvent));
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_ToQueuedClient()
        {
            var builder = new KustoOptionsBuilder();

            builder.RegisterType<OptionsTestEvent>().WithQueuedClient();

            builder.RegisteredQueuedTypes.Should().Contain(typeof(OptionsTestEvent));
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_False_UnknownTypeNoDefaults()
        {
            var builder = (KustoOptionsBuilder) new KustoOptionsBuilder().RegisterType<OptionsTestEvent>().WithQueuedClient()
                                                                         .RegisterType<OptionsTestEvent>().WithDirectClient();

            builder.RegisteredQueuedTypes.Should().Contain(typeof(OptionsTestEvent));
            builder.RegisteredDirectTypes.Should().Contain(typeof(OptionsTestEvent));
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_FallbackToDefaultClient()
        {
            var builder = (KustoOptionsBuilder)new KustoOptionsBuilder().RegisterType<OtherOptionsTestEvent>().WithDirectClient();

            builder.RegisteredDirectTypes.Should().Contain(typeof(OtherOptionsTestEvent));
        }

        public class OptionsTestEvent : TelemetryEvent { }

        public class OtherOptionsTestEvent : TelemetryEvent { }
    }
}