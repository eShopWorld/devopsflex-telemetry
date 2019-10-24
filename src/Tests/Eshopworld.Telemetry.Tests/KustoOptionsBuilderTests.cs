using Eshopworld.Core;
using Eshopworld.Telemetry.Kusto;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class KustoOptionsBuilderTests
    {
        [Theory, IsUnit]
        [InlineData(IngestionClient.Direct)]
        [InlineData(IngestionClient.Queued)]
        public void Test_IsRegistered_ToSpecificClient(IngestionClient client)
        {
            var builder = new KustoOptionsBuilder();

            if (client == IngestionClient.Queued)
                builder.RegisterType<OptionsTestEvent>().WithQueuedClient();
            else
                builder.RegisterType<OptionsTestEvent>().WithDirectClient();

            if (client == IngestionClient.Queued)
                builder.RegisteredQueuedTypes.Should().Contain(typeof(OptionsTestEvent));
            else
                builder.RegisteredDirectTypes.Should().Contain(typeof(OptionsTestEvent));
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
            var builder = (KustoOptionsBuilder) new KustoOptionsBuilder().RegisterType<OtherOptionsTestEvent>().WithDirectClient();

            builder.RegisteredDirectTypes.Should().Contain(typeof(OtherOptionsTestEvent));
        }

        public class OptionsTestEvent : TelemetryEvent { }

        public class OtherOptionsTestEvent : TelemetryEvent { }
    }
}