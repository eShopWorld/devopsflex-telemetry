using System;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherUseKustoTest
{
    [Fact, IsUnit]
    public void Test_ExceptionTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.SetupKustoSubscription();
        bb.Object.Publish(new Exception().ToExceptionEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_TimedTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.SetupKustoSubscription();
        bb.Object.Publish(new KustoTestTimedEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }
}
