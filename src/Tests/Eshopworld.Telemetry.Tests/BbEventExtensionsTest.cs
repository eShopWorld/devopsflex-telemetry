using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevOpsFlex.Core;
using DevOpsFlex.Tests.Core;
using Eshopworld.Telemetry;
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
            const string correlation = "my correlation";
            var tEvent = new TestTelemetryEvent { CorrelationVector = correlation };

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var result = tEvent.ToEventTelemetry();

            result.Should().NotBeNull();
            (result?.Context.Operation.CorrelationVector).Should().Be(correlation);
            (result?.Context.Operation.Id).Should().Be(correlation);
            (result?.Name).Should().Be(tEvent.GetType().Name);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
            (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
        }

        [Fact, IsUnit]
        public void Test_Timed_IsPopulated()
        {
            const string correlation = "my correlation";

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var tEvent = new TestTimedEvent { CorrelationVector = correlation };

            tEvent.End();
            var result = tEvent.ToTimedTelemetry();

            result.Should().NotBeNull();
            (result?.Context.Operation.CorrelationVector).Should().Be(correlation);
            (result?.Context.Operation.Id).Should().Be(correlation);
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
            const string correlation = "my correlation";

            var exception = new Exception(message);
            var tEvent = new TestExceptionEvent(exception) { CorrelationVector = correlation };

            var now = DateTime.Now;
            BbEventExtensions.Now = () => now;

            var result = tEvent.ToExceptionTelemetry();

            result.Should().NotBeNull();
            (result?.Context.Operation.CorrelationVector).Should().Be(correlation);
            (result?.Context.Operation.Id).Should().Be(correlation);
            (result?.Message).Should().Be(message);
            (result?.Exception).Should().Be(exception);
            (result?.Timestamp).Should().Be(now);
            (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
            (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
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
