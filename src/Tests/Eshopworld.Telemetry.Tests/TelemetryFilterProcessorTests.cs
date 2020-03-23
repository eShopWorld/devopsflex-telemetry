using System;
using Eshopworld.Telemetry.Processors;
using Eshopworld.Tests.Core;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class TelemetryFilterProcessorTests
    {
        private readonly Mock<ITelemetryProcessor> nextProcessorMock;
        private readonly Mock<ITelemetryFilterCriteria> filterCriteriaMock;
        private readonly TelemetryFilterProcessor filterProcessor;

        public TelemetryFilterProcessorTests()
        {
            nextProcessorMock = new Mock<ITelemetryProcessor>();
            filterCriteriaMock = new Mock<ITelemetryFilterCriteria>();

            filterProcessor = new TelemetryFilterProcessor(nextProcessorMock.Object, filterCriteriaMock.Object);
        }


        [Fact, IsUnit]
        public void FilterProcess_WithNullNextProcessor_ThrowsException()
        {
            // Arrange

            // Act
            var exception = Record.Exception(() => new TelemetryFilterProcessor(null, filterCriteriaMock.Object));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact, IsUnit]
        public void FilterProcess_WithNullFilterCriteria_ThrowsException()
        {
            // Arrange

            // Act
            var exception = Record.Exception(() => new TelemetryFilterProcessor(nextProcessorMock.Object, null));

            // Assert
            Assert.NotNull(exception);
            Assert.IsType<ArgumentNullException>(exception);
        }


        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Process_ExecutesNextProcessor_BasedOnFilterCriteria(bool filter)
        {
            // Arrange
            filterCriteriaMock.Setup(x => x.ShouldFilter(It.IsAny<ITelemetry>())).Returns(filter);

            // Act
            filterProcessor.Process(new RequestTelemetry());

            // Assert
            nextProcessorMock.Verify(x => x.Process(It.IsAny<ITelemetry>()),
                filter ? Times.Never() : Times.Once());
        }
    }
}
