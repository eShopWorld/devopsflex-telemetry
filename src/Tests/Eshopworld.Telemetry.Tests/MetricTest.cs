using System;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

// ReSharper disable once CheckNamespace
public class MetricTest
{
    private static readonly string InstrumentationKey = Environment.GetEnvironmentVariable("devai") ?? "";

    public class GetMetricDimensions
    {
        [Fact, IsUnit]
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

        [Fact, IsUnit]
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
        [Fact, IsUnit]
        public void Test_GetTestMetric()
        {
            var client = new TelemetryClient(new TelemetryConfiguration(""));
            var expectedDimensions = typeof(TestMetric).GetMetricDimensions();

            var metric = client.InvokeGetMetric<TestMetric>();

            metric.Identifier.MetricId.Should().Be(nameof(TestMetric));
            metric.Identifier.GetDimensionNames().Should().BeEquivalentTo(expectedDimensions.Select(p => p.Name));

            metric.TrackValue(999, "Dim1", "Dim2", "Dim3");
        }
    }

    public class GenerateExpressionTrackValue
    {
        /// <remarks>
        /// Currently there's no way to assert that a <see cref="Metric"/> tracks events, so this test is only half-useful:
        ///     It only validates the integrity of the expression tree, not it's behaviour.
        /// </remarks>
        [Fact, IsUnit]
        public void Test_TrackValue_WithSingleValue()
        {
            var testMetric = new TestMetric(Lorem.GetWord(), Lorem.GetWord(), Lorem.GetWord());
            var client = new TelemetryClient(new TelemetryConfiguration(""));
            var metric = client.InvokeGetMetric<TestMetric>();

            var func = typeof(TestMetric).GenerateExpressionTrackValue();
            testMetric.Metric = 999;
            func(metric, testMetric);

            client.Flush();
        }

        /// <remarks>
        /// Currently there's no way to assert that a <see cref="Metric"/> tracks events, so this test is only half-useful:
        ///     It only validates the integrity of the expression tree, not it's behaviour.
        /// </remarks>
        [Fact, IsUnit]
        public void Test_TrackValue_WithZeroDimensions()
        {
            var testMetric = new TestMetricZeroDimensions();
            var client = new TelemetryClient(new TelemetryConfiguration(""));
            var metric = client.InvokeGetMetric<TestMetricZeroDimensions>();

            var func = typeof(TestMetricZeroDimensions).GenerateExpressionTrackValue();
            testMetric.Metric = 999;
            func(metric, testMetric);
        }
    }

    [Fact, IsUnit]
    public void Test_Metric_NonVirtualOverride()
    {
        var bb = new BigBrother("", "");
        var action = new Action(() => bb.GetTrackedMetric<TestMetricNonVirtual>());

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact, IsDev]
    public async Task Test_MetricPush_10Samples()
    {
        var rng = new Random();
        var bb = new BigBrother(InstrumentationKey, InstrumentationKey);
        var metric = bb.GetTrackedMetric<TestMetric>();
        metric.DimensionOne = Lorem.GetWord();
        metric.DimensionTwo = Lorem.GetWord();
        metric.DimensionThree = Lorem.GetWord();

        for (int i = 0; i < 10; i++)
        {
            metric.Metric = rng.Next(100);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        bb.Flush();
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

public class TestMetric : ITrackedMetric
{
    public TestMetric() { }

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

    public int Useless { get; set; }
}

public class TestMetricZeroDimensions : ITrackedMetric
{
    public virtual double Metric { get; set; }
}

public class TestMetricNonVirtual : ITrackedMetric
{
    public double Metric { get; set; }
}