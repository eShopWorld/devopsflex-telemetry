using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Telemetry.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Eshopworld.Telemetry.Tests.Configuration
{
    public class LogicalCallTelemetryInitializerTests
    {
        [Fact, IsLayer0]
        public void SetsProperties()
        {
            var events = new ConcurrentBag<ITelemetry>();
            var builder = new TelemetryClientBuilder();
            builder.AddInitializer(LogicalCallTelemetryInitializer.Instance);
            var telemetryClient = builder.Build(e => events.Add(e));
            LogicalCallTelemetryInitializer.Instance.SetProperty("TestProperty", "TestValue");

            telemetryClient.TrackEvent("test");
            telemetryClient.Flush();

            events.Should().HaveCount(1);
            events.First().Should().BeOfType<EventTelemetry>()
                .Which.Properties.Should().ContainKey("TestProperty")
                .WhichValue.Should().Be("TestValue");
        }

        [Fact, IsLayer0]
        public async Task SetsPropertiesPerLogicalThread()
        {
            var events = new ConcurrentBag<ITelemetry>();
            var builder = new TelemetryClientBuilder();
            builder.AddInitializer(LogicalCallTelemetryInitializer.Instance);
            var telemetryClient = builder.Build(e => events.Add(e));

            var tasks = new[] { SetupLogicalThread("1"), SetupLogicalThread("2") };
            await Task.WhenAll(tasks);
            telemetryClient.Flush();
            await Task.Delay(100);

            events.Should().HaveCount(2);
            events.OfType<EventTelemetry>()
                .FirstOrDefault(x => x.Properties.TryGetValue("TestName", out var value) && value == "1").Should().NotBeNull();
            events.OfType<EventTelemetry>()
                .FirstOrDefault(x => x.Properties.TryGetValue("TestName", out var value) && value == "2").Should().NotBeNull();

            async Task SetupLogicalThread(string name)
            {

                await Task.Yield();
                LogicalCallTelemetryInitializer.Instance.SetProperty("TestName", name);
                await Task.Yield();
                telemetryClient.TrackEvent("test " + name);
            }
        }
    }
}
