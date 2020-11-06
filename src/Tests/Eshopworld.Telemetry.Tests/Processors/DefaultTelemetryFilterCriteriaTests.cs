using Eshopworld.Telemetry.Processors;
using Eshopworld.Tests.Core;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Eshopworld.Telemetry.Tests.Processors
{
    public class DefaultTelemetryFilterCriteriaTests
    {
        [Fact, IsUnit]
        public void ShouldFilter_Always_ReturnsFalse()
        {
            // Arrange

            // Act
            var result = new DefaultTelemetryFilterCriteria().ShouldFilter(new RequestTelemetry());

            // Assert
            Assert.False(result);
        }
    }
}
