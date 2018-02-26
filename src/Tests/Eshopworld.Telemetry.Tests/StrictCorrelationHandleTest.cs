using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class StrictCorrelationHandleTest
{
    public class Dispose
    {
        [Fact, IsUnit]
        public void Ensure_HandleIsAssignedAndReleased()
        {
            var bbMock = new Mock<BigBrother>();
            using (var handle = new StrictCorrelationHandle(bbMock.Object))
            {
                bbMock.Object.Handle.Should().Be(handle);
            }

            bbMock.Object.Handle.Should().BeNull();
        }
    }
}
