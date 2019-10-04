using System;
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
                builder.WithQueuedClient<OptionsTestEvent>();
            else
                builder.WithDirectClient<OptionsTestEvent>();

            builder.IsRegisteredOrDefault(client, typeof(OptionsTestEvent)).Should().BeTrue();
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_False_UnknownTypeNoDefaults()
        {
            var builder = new KustoOptionsBuilder().WithQueuedClient<OptionsTestEvent>().WithDirectClient<OptionsTestEvent>();

            builder.IsRegisteredOrDefault(IngestionClient.Queued, typeof(OtherOptionsTestEvent)).Should().BeFalse();
            builder.IsRegisteredOrDefault(IngestionClient.Direct, typeof(OtherOptionsTestEvent)).Should().BeFalse();
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_FallbackToDefaultClient()
        {
            var builder = new KustoOptionsBuilder().WithFallbackDirectClient();

            builder.IsRegisteredOrDefault(IngestionClient.Direct, typeof(OtherOptionsTestEvent)).Should().BeTrue();
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_RegistrationOverridesDefault()
        {
            var builder = new KustoOptionsBuilder().WithQueuedClient<OptionsTestEvent>().WithFallbackDirectClient();

            builder.IsRegisteredOrDefault(IngestionClient.Queued, typeof(OptionsTestEvent)).Should().BeTrue();
            builder.IsRegisteredOrDefault(IngestionClient.Direct, typeof(OptionsTestEvent)).Should().BeFalse();
        }

        [Fact, IsUnit]
        public void Test_IsRegistered_TwoDefaults_ThrowException()
        {
            var builder = new KustoOptionsBuilder();

            builder
                .Invoking(b => b.WithFallbackDirectClient().WithFallbackQueuedClient())
                .Should().Throw<InvalidOperationException>();
        }

        public class OptionsTestEvent : TelemetryEvent { }
        public class OtherOptionsTestEvent : TelemetryEvent { }
    }
}