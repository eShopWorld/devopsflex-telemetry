using System;
using System.Text;
using System.Threading.Tasks;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once CheckNamespace
public class CorrelationHandleTest
{
    public class ToBase64
    {
        [Fact, IsUnit]
        public void Test_EncodingAGuid()
        {
            var guid = Guid.NewGuid();

            var result = guid.ToBase64();

            Encoding.Default.GetString(Convert.FromBase64String(result)).Should().Be(guid.ToString());
        }

        [Fact, IsUnit]
        public async Task Test_KeepAlive1Minute()
        {
            var handle = new CorrelationHandle(1);

            await Task.Delay(TimeSpan.FromSeconds(30));

            handle.IsAlive(DateTime.Now).Should().BeTrue();

            await Task.Delay(TimeSpan.FromSeconds(30));

            handle.IsAlive(DateTime.Now).Should().BeFalse();
        }

        [Fact, IsUnit]
        public async Task Test_KeepAlive1Minute_PlusTouch()
        {
            var handle = new CorrelationHandle(1);

            await Task.Delay(TimeSpan.FromSeconds(30));
            handle.IsAlive(DateTime.Now).Should().BeTrue();

            await Task.Delay(TimeSpan.FromSeconds(30));
            handle.IsAlive(DateTime.Now).Should().BeFalse();

            handle.Touch();

            await Task.Delay(TimeSpan.FromSeconds(30));
            handle.IsAlive(DateTime.Now).Should().BeTrue();

            await Task.Delay(TimeSpan.FromSeconds(30));
            handle.IsAlive(DateTime.Now).Should().BeFalse();
        }
    }
}
