using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once CheckNamespace
public class BbEventExtensionsTest
{
    public class ToTelemetry
    {
        [Fact, IsUnit]
        public void Test_Event_IsPopulated()
        {
            var tEvent = new TestTelemetryEvent();

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var result = tEvent.ToEventTelemetry();

            result.Should().NotBeNull();
            (result?.Name).Should().Be(tEvent.GetType().Name);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
            (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
        }

        [Fact, IsUnit]
        public void Test_Timed_IsPopulated()
        {
            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var tEvent = new TestTimedEvent();

            tEvent.End();
            var result = tEvent.ToTimedTelemetry();

            result.Should().NotBeNull();
            (result?.Name).Should().Be(tEvent.GetType().Name);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
            (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
            (result?.Metrics[nameof(BbTimedEvent.ProcessingTime)]).Should().Be(tEvent.ProcessingTime.TotalSeconds);
        }

        [Fact, IsUnit]
        public void Test_Exception_IsPopulated()
        {
            const string message = "KABUM!!!";

            var exception = new Exception(message);
            var tEvent = new TestExceptionEvent(exception);

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var result = tEvent.ToExceptionTelemetry();

            result.Should().NotBeNull();
            (result?.Message).Should().Be(message);
            (result?.Exception).Should().Be(exception);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
            (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
        }

        [Fact, IsUnit]
        public void Foo()
        {
            const string caller = "just a caller!";

            var payload = new
            {
                AName = "some name",
                AValue = 123
            };

            var tEvent = new BbAnonymousEvent(payload) { CallerMemberName = caller };

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var result = tEvent.ToAnonymousTelemetry();

            result.Should().NotBeNull();
            (result?.Name).Should().Be(tEvent.CallerMemberName);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(payload.AName)]).Should().Be(payload.AName);
            (result?.Properties[nameof(payload.AValue)]).Should().Be(payload.AValue.ToString());
        }

        [Fact, IsUnit]
        public async Task Test_Event_PublishesOnException()
        {
            var exceptionFired = false;

            using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                             .Subscribe(e =>
                             {
                                 e.Exception.Should().NotBeNull();
                                 exceptionFired = true;
                             }))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = BbEventExtensions.ToEventTelemetry(null);
                result.Should().BeNull();

                await Task.Delay(TimeSpan.FromSeconds(1));

                exceptionFired.Should().BeTrue();
            }
        }

        [Fact, IsUnit]
        public async Task Test_Exception_PublishesOnException()
        {
            var exceptionFired = false;

            using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                             .Subscribe(e =>
                             {
                                 e.Exception.Should().NotBeNull();
                                 exceptionFired = true;
                             }))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = BbEventExtensions.ToExceptionTelemetry(null);
                result.Should().BeNull();

                await Task.Delay(TimeSpan.FromSeconds(1));

                exceptionFired.Should().BeTrue();
            }
        }

        [Fact, IsUnit]
        public async Task Test_Timed_PublishesOnException()
        {
            var exceptionFired = false;

            using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                             .Subscribe(e =>
                             {
                                 e.Exception.Should().NotBeNull();
                                 exceptionFired = true;
                             }))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = BbEventExtensions.ToTimedTelemetry(null);
                result.Should().BeNull();

                await Task.Delay(TimeSpan.FromSeconds(1));

                exceptionFired.Should().BeTrue();
            }
        }
    }
}
