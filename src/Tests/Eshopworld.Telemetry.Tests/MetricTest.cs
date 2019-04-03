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
    public class GetMetricDimensions
    {
        [Fact, IsDev]
        public void Test_TestMetric_WithThreeDimensions()
        {
            var dimensions = typeof(TestMetric).GetMetricDimensions()
                                               .Select(p => p.Name)
                                               .ToList();

            dimensions.Should().HaveCount(3);
            dimensions.Should().Contain(nameof(TestMetric.DimensionOne));
            dimensions.Should().Contain(nameof(TestMetric.DimensionTwo));
            dimensions.Should().Contain(nameof(TestMetric.DimensionThree));
        }

        [Fact, IsDev]
        public void Test_TestMetric_WithZeroDimensions()
        {
            var dimensions = typeof(TestMetricZeroDimensions).GetMetricDimensions()
                                                             .Select(p => p.Name)
                                                             .ToList();

            dimensions.Should().BeEmpty();
        }
    }

    public class InvokeGetMetric
    {
        [Fact, IsDev]
        public void Test_GetTestMetric()
        {
            var client = new TelemetryClient();
            var expectedDimensions = typeof(TestMetric).GetMetricDimensions();

            var metric = client.InvokeGetMetric<TestMetric>();

            metric.Identifier.MetricId.Should().Be(nameof(TestMetric));
            metric.Identifier.GetDimensionNames().Should().BeEquivalentTo(expectedDimensions.Select(p => p.Name));
        }
    }

    public class GenerateExpressionTrackValue
    {
        [Fact, IsDev]
        public void Test_TrackValue_WithSingleValue()
        {
            var testMetric = new TestMetric(Lorem.GetWord(), Lorem.GetWord(), Lorem.GetWord());
            var client = new TelemetryClient();
            var metric = client.InvokeGetMetric<TestMetric>();

            var func = typeof(TestMetric).GenerateExpressionTrackValue();
            testMetric.Metric = 999;
            func(metric, testMetric);
        }
    }
}

public class TestMetric : ITrackedMetric
{
    public TestMetric(string dimensionOne, string dimensionTwo, string dimensionThree)
    {
        DimensionOne = dimensionOne;
        DimensionTwo = dimensionTwo;
        DimensionThree = dimensionThree;
    }

    public virtual double Metric { get; set; }

    public string DimensionOne { get; set; }

    public string DimensionTwo { get; set; }

    public string DimensionThree { get; set; }

    public int Crazy { get; set; }
}

public class TestMetricZeroDimensions : ITrackedMetric
{
    public virtual double Metric { get; set; }
}