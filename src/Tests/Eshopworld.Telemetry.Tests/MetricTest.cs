using System.Linq;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Xunit;

// ReSharper disable once CheckNamespace
public class MetricTest
{
    public class GenerateExpressionGetMetric
    {
        [Fact, IsDev]
        public void Test_GetTestMetric()
        {
            var client = new TelemetryClient();
            var expectedDimensions = typeof(TestMetric).GetProperties()
                                                       .Where(
                                                           p =>
                                                               p.Name != nameof(ITrackedMetric.Metric)
                                                               && p.GetMethod.IsPublic
                                                               && p.GetMethod.ReturnType == typeof(string)
                                                       )
                                                       .Select(p => p.Name)
                                                       .ToList();

            var func = typeof(TestMetric).GenerateExpressionGetMetric();
            var metric = func(client, typeof(TestMetric));

            metric.Identifier.MetricId.Should().Be(nameof(TestMetric));
            metric.Identifier.GetDimensionNames().Should().BeEquivalentTo(expectedDimensions);
        }
    }
}

public class TestMetric : ITrackedMetric
{
    public double Metric { get; set; }

    public string DimensionOne { get; set; }

    public string DimensionTwo { get; set; }

    public string DimensionThree { get; set; }

    public int Crazy { get; set; }
}
