using Eshopworld.Telemetry.Processors;
using Eshopworld.Tests.Core;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class SuccessfulProbeFilterCriteriaTests
    {
        [Fact, IsUnit]
        public void ShouldFilter_WhenNonRequestTelemetry_ReturnsFalse()
        {
            // Arrange
            var telemetryItem = new DependencyTelemetry();

            var criteria = new SuccessfulProbeFilterCriteria();

            // Act
            var result = criteria.ShouldFilter(telemetryItem);

            // Assert
            Assert.False(result);
        }


        [Theory, IsUnit]
        [InlineData("200", "GET Probe/Probe", true)]
        [InlineData("200", "get Probe/Probe", true)]
        [InlineData("200", "GET probe/Probe", true)]
        [InlineData("200", "GET Probe/ProbeUserContext", true)]
        [InlineData("400", "GET Probe/Probe", false)]
        [InlineData("200", "PUT Probe/Probe", false)]
        [InlineData("200", "GET Entity/Probe", false)]
        [InlineData("200", "GET ProbeTest/Probe", false)]
        [InlineData("200", "HEAD Probe/Probe", true)]
        [InlineData("200", "head Probe/Probe", true)]
        [InlineData("200", "HEAD probe/Probe", true)]
        [InlineData("200", "HEAD Probe/ProbeUserContext", true)]
        [InlineData("400", "HEAD Probe/Probe", false)]
        [InlineData("200", "HEAD Entity/Probe", false)]
        [InlineData("200", "HEAD ProbeTest/Probe", false)]
        public void Test(string responseCode, string actionName, bool shouldFilter)
        {
            // Arrange
            var telemetryItem = new RequestTelemetry { Name = actionName, ResponseCode = responseCode };
            var criteria = new SuccessfulProbeFilterCriteria();

            // Act
            var result = criteria.ShouldFilter(telemetryItem);

            // Assert
            Assert.Equal(shouldFilter, result);
        }
    }
}
