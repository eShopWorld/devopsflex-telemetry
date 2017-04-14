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
        public void Test_Exception_IsPopulated()
        {
            using (ShimsContext.Create())
            {
                const string message = "KABUM!!!";
                var exception = new Exception(message);
                var tEvent = new TestExceptionEvent
                {
                    Exception = exception
                };

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
                var tEvent = new TestExceptionEvent
                {
                    Exception = new Exception("KABUM!!!")
                };

                var wrontEventType = false;
                ShimTelemetryExtensions.SetCorrelationITelemetryBbTelemetryEvent = (t, e) => throw exception;
                BigBrother.InternalStream.Subscribe(e =>
                {
                    if (e is BbExceptionEvent exEvent)
                        exEvent.Exception.Should().Be(exception);
                    else
                        wrontEventType = true;
                });

                var result = tEvent.ToTelemetry();
                result.Should().BeNull();
                wrontEventType.Should().BeFalse();
            }
        }
    }
}
