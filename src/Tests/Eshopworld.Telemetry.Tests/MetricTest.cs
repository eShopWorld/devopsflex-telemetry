using Eshopworld.Core;
using Eshopworld.Tests.Core;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class MetricTest
    {
        [Fact, IsDev]
        public void Foo()
        {
            var bb = new BigBrother("", "");
            bb.GetTrackedMetric<TestMetric>();
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
}
