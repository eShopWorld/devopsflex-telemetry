using System;
using Eshopworld.Core;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Moq;
using Xunit;

// ReSharper disable InvokeAsExtensionMethod
namespace Eshopworld.Telemetry.Tests
{
    public class TelemetryConfigurationExtensionsTests
    {
        private readonly Mock<BigBrother> _mockBb;

        public TelemetryConfigurationExtensionsTests()
        {
            _mockBb = new Mock<BigBrother>();
        }

        [Fact, IsUnit]
        public void UseEventSourceSink_OnCastingError_ThrowsException()
        {
            Action action = () => TelemetryConfigurationExtensions.UseEventSourceSink(Mock.Of<IBigBrother>());
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Couldn't cast this instance of *IBigBrother to a concrete implementation of *BigBrother");
        }

        [Fact, IsUnit]
        public void UseEventSourceSink_OnSuccess_ReturnsEventSink()
        {
            TelemetryConfigurationExtensions.UseEventSourceSink(_mockBb.Object)
                .Should().BeOfType<EventSourceSink>()
                .Which.Bb.Should().Be(_mockBb.Object);
        }

        [Fact, IsUnit]
        public void UseTraceSink_OnCastingError_ReturnsTraceSink()
        {
            Action action = () => TelemetryConfigurationExtensions.UseTraceSink(Mock.Of<IBigBrother>());
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Couldn't cast this instance of *IBigBrother to a concrete implementation of *BigBrother");
        }

        [Fact, IsUnit]
        public void UseTraceSink_OnSuccess_ReturnsTraceSink()
        {
            TelemetryConfigurationExtensions.UseTraceSink(_mockBb.Object)
                .Should().BeOfType<TraceSink>()
                .Which.Bb.Should().Be(_mockBb.Object);
        }

        [Fact, IsUnit]
        public void ForExceptions_WithEventSourceSink_DisposesAndRecreates()
        {
            var sink = _mockBb.Object.UseEventSourceSink() as EventSourceSink;
            var mockDisposable = new Mock<IDisposable>();
            _mockBb.Object.EventSourceSinkSubscription = mockDisposable.Object;

            // Act
            sink!.ForExceptions();

            // Assert
            mockDisposable.Verify(x=>x.Dispose(), Times.Once);
            sink
                .Should().BeOfType<EventSourceSink>()
                .Which.Bb.EventSourceSinkSubscription.Should().NotBe(mockDisposable.Object);
        }

        [Fact, IsUnit]
        public void ForExceptions_WithTraceSink_DisposesAndRecreates()
        {
            var sink = _mockBb.Object.UseTraceSink() as TraceSink;
            var mockDisposable = new Mock<IDisposable>();
            _mockBb.Object.TraceSinkSubscription = mockDisposable.Object;

            // Act
            sink!.ForExceptions();

            // Assert
            mockDisposable.Verify(x => x.Dispose(), Times.Once);
            sink
                .Should().BeOfType<TraceSink>()
                .Which.Bb.TraceSinkSubscription.Should().NotBe(mockDisposable.Object);
        }
    }
}
