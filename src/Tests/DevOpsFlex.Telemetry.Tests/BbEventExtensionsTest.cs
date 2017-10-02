using System;
using System.Fakes;
using System.Reactive.Linq;
using DevOpsFlex.Core;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Telemetry.Fakes;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Microsoft.QualityTools.Testing.Fakes;
using Xunit;

// ReSharper disable once CheckNamespace
public class BbEventExtensionsTest
{
    public class ToTelemetry
    {
        [Fact, IsUnit]
        public void Test_Event_IsPopulated()
        {
            using (ShimsContext.Create())
            {
                var tEvent = new TestTelemetryEvent();

                var now = DateTime.Now;
                ShimDateTime.NowGet = () => now;

                var correlationSet = false;
                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) =>
                {
                    if (e == tEvent)
                        correlationSet = true;
                };

                var result = tEvent.ToTelemetry();

                result.Should().NotBeNull();
                correlationSet.Should().BeTrue();
                (result?.Name).Should().Be(tEvent.GetType().Name);
                (result?.Timestamp).Should().Be(now);
                (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
                (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
            }
        }

        [Fact, IsUnit]
        public void Test_Timed_IsPopulated()
        {
            using (ShimsContext.Create())
            {
                var now = DateTime.Now;
                var nowPlus10 = DateTime.Now.AddMinutes(10);
                ShimDateTime.NowGet = () => now;

                var tEvent = new TestTimedEvent();
                ShimDateTime.NowGet = () => nowPlus10;

                var correlationSet = false;
                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) =>
                {
                    if (e == tEvent)
                        correlationSet = true;
                };

                var result = tEvent.ToTelemetry();

                result.Should().NotBeNull();
                correlationSet.Should().BeTrue();
                (result?.Name).Should().Be(tEvent.GetType().Name);
                (result?.Timestamp).Should().Be(nowPlus10);
                (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
                (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
                (result?.Metrics[nameof(BbTimedEvent.ProcessingTime)]).Should().BeApproximately(600, 6);
            }
        }

        [Fact, IsUnit]
        public void Test_Exception_IsPopulated()
        {
            using (ShimsContext.Create())
            {
                const string message = "KABUM!!!";
                var exception = new Exception(message);
                var tEvent = new TestExceptionEvent(exception);

                var now = DateTime.Now;
                ShimDateTime.NowGet = () => now;

                var correlationSet = false;
                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) =>
                {
                    if (e == tEvent)
                        correlationSet = true;
                };

                var result = tEvent.ToTelemetry();

                result.Should().NotBeNull();
                correlationSet.Should().BeTrue();
                (result?.Message).Should().Be(message);
                (result?.Exception).Should().Be(exception);
                (result?.Timestamp).Should().Be(now);
                (result?.Properties[nameof(TestExceptionEvent.Id)]).Should().Be(tEvent.Id.ToString());
                (result?.Properties[nameof(TestExceptionEvent.Description)]).Should().Be(tEvent.Description);
            }
        }

        [Fact, IsUnit]
        public void Test_Event_PublishesOnException()
        {
            using (ShimsContext.Create())
            {
                var exception = new Exception("Exploding the test here");
                var tEvent = new TestTelemetryEvent();

                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) => throw exception;
                using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                                 .Subscribe(e =>
                                 {
                                     e.Exception.Should().Be(exception);
                                 }))
                {
                    var result = tEvent.ToTelemetry();
                    result.Should().BeNull();
                }
            }
        }

        [Fact, IsUnit]
        public void Test_Exception_PublishesOnException()
        {
            using (ShimsContext.Create())
            {
                var exception = new Exception("Exploding the test here");
                var tEvent = new TestExceptionEvent(new Exception("KABUM!!!"));

                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) => throw exception;
                using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                                 .Subscribe(e =>
                                 {
                                     e.Exception.Should().Be(exception);
                                 }))
                {
                    var result = tEvent.ToTelemetry();
                    result.Should().BeNull();
                }
            }
        }

        [Fact, IsUnit]
        public void Test_Timed_PublishesOnException()
        {
            using (ShimsContext.Create())
            {
                var exception = new Exception("Exploding the test here");
                var tEvent = new TestTimedEvent();

                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) => throw exception;
                using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                                 .Subscribe(e =>
                                 {
                                     e.Exception.Should().Be(exception);
                                 }))
                {
                    var result = tEvent.ToTelemetry();
                    result.Should().BeNull();
                }
            }
        }
    }
}
