using System.Collections.Generic;
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
        [MemberData(nameof(TelemetryRequestTestData))]
        public void Test(string responseCode, string actionName, string healthCheckPath, bool shouldFilter)
        {
            // Arrange
            var telemetryItem = new RequestTelemetry {Name = actionName, ResponseCode = responseCode};
            var criteria = new SuccessfulProbeFilterCriteria(healthCheckPath);

            // Act
            var result = criteria.ShouldFilter(telemetryItem);

            // Assert
            Assert.Equal(shouldFilter, result);
        }

        public static IEnumerable<object[]> TelemetryRequestTestData => new List<object[]>
        {
            new object[]
            {
                "200",
                "GET Probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "get Probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "GET probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "GET Probe/ProbeUserContext",
                null,
                true
            },
            new object[]
            {
                "400",
                "GET Probe/ProbeUserContext",
                null,
                false
            },
            new object[]
            {
                "200",
                "PUT Probe/ProbeUserContext",
                null,
                false
            },
            new object[]
            {
                "400",
                "GET Entity/Probe",
                null,
                false
            },
            new object[]
            {
                "400",
                "GET ProbeTest/Probe",
                null,
                false
            },
            new object[]
            {
                "200",
                "HEAD Probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "head Probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "HEAD probe/Probe",
                null,
                true
            },
            new object[]
            {
                "200",
                "HEAD Probe/ProbeUserContext",
                null,
                true
            },
            new object[]
            {
                "400",
                "HEAD Probe/ProbeUserContext",
                null,
                false
            },
            new object[]
            {
                "400",
                "HEAD Entity/Probe",
                null,
                false
            },
            new object[]
            {
                "400",
                "HEAD ProbeTest/Probe",
                null,
                false
            },
            new object[]
            {
                "200",
                "GET Probe/Probe",
                "",
                true
            },
            new object[]
            {
                "200",
                "GET Probe/Probe",
                "/hc",
                true
            },
            new object[]
            {
                "200",
                "GET /HC",
                "/hc",
                true
            },
            new object[]
            {
                "200",
                "GET /hc",
                "/hc",
                true
            },
            new object[]
            {
                "400",
                "GET Probe/Probe",
                "/hc",
                false
            },
            new object[]
            {
                "400",
                "GET /HC",
                "/hc",
                false
            },
            new object[]
            {
                "400",
                "GET /hc",
                "/hc",
                false
            },new object[]
            {
                "200",
                "PUT /HC",
                "/hc",
                false
            },
            new object[]
            {
                "200",
                "PUT /hc",
                "/hc",
                false
            },
            new object[]
            {
                "400",
                "PUT Probe/Probe",
                "/hc",
                false
            }
        };
    }
}