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

        [Fact, IsUnit]
        public void Test_With_Single_Matching_CheckPath()
        {
            // Arrange
            var path = "/testPath";
            var action = $"GET {path}";

            var telemetryItem = new RequestTelemetry{Name = action, ResponseCode = "200"};

            var criteria = new SuccessfulProbeFilterCriteria(path);

            // Act
            var result = criteria.ShouldFilter(telemetryItem);

            // Assert
            Assert.True(result);
        }


        [Theory, IsUnit]
        [MemberData(nameof(TelemetryRequestTestData))]
        public void Test_With_Multiple_CheckPaths(string responseCode, string actionName, bool shouldFilter, params string[] healthCheckPaths)
        {
            // Arrange
            var telemetryItem = new RequestTelemetry {Name = actionName, ResponseCode = responseCode};
            var criteria = new SuccessfulProbeFilterCriteria(healthCheckPaths);

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
                "HEAD /Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "GET /Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "HEAD /probe/ProbeContext",
                true,
                null
            },
            new object[]
            {
                "200",
                "GET /Probe/ProbeContext",
                true,
                null
            },
            new object[]
            {
                "200",
                "GET /Probe",
                true,
                "/hc","/hhc"
            },
            new object[]
            {
                "200",
                "HEAD /HHC",
                true,
                "/hc","/hhc"
            },
            new object[]
            {
                "200",
                "HEAD /HHC",
                false,
                "/foo","/bar","probe/","/probe"
            },
            new object[]
            {
                "200",
                "GET Probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "get Probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "GET probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "GET Probe/ProbeUserContext",
                true,
                null
            },
            new object[]
            {
                "400",
                "GET Probe/ProbeUserContext",
                false,
                null
            },
            new object[]
            {
                "200",
                "PUT Probe/ProbeUserContext",
                false,
                null
            },
            new object[]
            {
                "400",
                "GET Entity/Probe",
                false,
                null
            },
            new object[]
            {
                "400",
                "GET ProbeTest/Probe",
                false,
                null
            },
            new object[]
            {
                "200",
                "HEAD Probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "head Probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "HEAD probe/Probe",
                true,
                null
            },
            new object[]
            {
                "200",
                "HEAD Probe/ProbeUserContext",
                true,
                null
            },
            new object[]
            {
                "400",
                "HEAD Probe/ProbeUserContext",
                false,
                null
            },
            new object[]
            {
                "400",
                "HEAD Entity/Probe",
                false,
                null
            },
            new object[]
            {
                "400",
                "HEAD ProbeTest/Probe",
                false,
                null
            },
            new object[]
            {
                "200",
                "GET Probe/Probe",
                true,
                ""
            },
            new object[]
            {
                "200",
                "GET Probe/Probe",
                true,
                "/hc"
            },
            new object[]
            {
                "200",
                "GET /HC",
                true,
                "/hc"
            },
            new object[]
            {
                "200",
                "GET /hc",
                true,
                "/hc"
            },
            new object[]
            {
                "400",
                "GET Probe/Probe",
                false,
                "/hc"
            },
            new object[]
            {
                "400",
                "GET /HC",
                false,
                "/hc"
            },
            new object[]
            {
                "400",
                "GET /hc",
                false,
                "/hc"
            },new object[]
            {
                "200",
                "PUT /HC",
                false,
                "/hc"
            },
            new object[]
            {
                "200",
                "PUT /hc",
                false,
                "/hc"
            },
            new object[]
            {
                "400",
                "PUT Probe/Probe",
                false,
                "/hc"
            }
        };
    }
}