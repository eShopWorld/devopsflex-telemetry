using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

// ReSharper disable once CheckNamespace
public class TelemetryExtensionsTest
{
    public class SetCorrelation
    {
        [Fact, IsUnit]
        public void Test_EventTelemetry_OperationIsPoPulated()
        {
            const string correlationVector = "SOMEIDHERE.1.3";
            var tEvent = new EventTelemetry();
            var bbEvent = new BbTelemetryEvent
            {
                CorrelationVector = correlationVector
            };

            tEvent.SetCorrelation(bbEvent);

            tEvent.Context.Operation.CorrelationVector.Should().Be(correlationVector);
            tEvent.Context.Operation.Id.Should().Be(correlationVector);
        }

        [Fact, IsUnit]
        public void Ensure_NullCorrelation_DoesntPopulate()
        {
            const string correlationVector = null;
            var tEvent = new EventTelemetry();
            var bbEvent = new BbTelemetryEvent
            {
                CorrelationVector = correlationVector
            };

            tEvent.SetCorrelation(bbEvent);

            tEvent.Context.Operation.CorrelationVector.Should().BeNull();
            tEvent.Context.Operation.Id.Should().BeNull();
        }
    }
}
