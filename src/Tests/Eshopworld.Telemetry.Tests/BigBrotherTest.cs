using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Diagnostics.Tracing.Session;
using Moq;
using Xunit;

//ReSharper disable once CheckNamespace
public class BigBrotherTest
{
    internal static readonly string DevKey = Environment.GetEnvironmentVariable("devai", EnvironmentVariableTarget.User);

    public class Dev
    {
        [Fact, IsDev]
        public void EntryPoint_PushEvent()
        {
            var bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(new TestTelemetryEvent());
            bb.Flush();
        }

        [Fact, IsDev]
        public void EntryPoint_PushException()
        {
            const string message = "KABOOM!!!";
            var bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            try
            {
                BlowUp(message);
            }
            catch (Exception ex)
            {
                bb.Publish(ex.ToBbEvent());
                bb.Flush();
            }
        }

        [Fact, IsDev]
        public void EntryPoint_PushTimed()
        {
            var bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(new TestTimedEvent());
            bb.Flush();
        }

    }

    public class Publish
    {
        [Fact, IsUnit]
        public async Task Test_PublishWithoutCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };
            var tEvent = new TestTelemetryEvent();

            TestTelemetryEvent rEvent = null;
            using (bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e))
            {
                bbMock.Object.Publish(tEvent);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

                rEvent.Should().NotBeNull();
                rEvent.Should().Be(tEvent);
                rEvent.CorrelationVector.Should().BeNull();
            }
        }

        [Fact, IsUnit]
        public async Task Test_PublishUnderStrictCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };

            TestTelemetryEvent rEvent = null;
            using (bbMock.Object.CreateCorrelation())
            using (bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e))
            {
                var tEvent = new TestTelemetryEvent();

                bbMock.Object.Publish(tEvent);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

                rEvent.Should().NotBeNull();
                rEvent.Should().Be(tEvent);
                bbMock.Object.Handle.Should().NotBeNull();
                rEvent.CorrelationVector.Should().Be(bbMock.Object.Handle.Vector);
            }
        }

        [Fact, IsUnit]
        public async Task Test_PublishUnderLoseCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };
            var handle = new object();
            var tEvent = new TestTelemetryEvent();

            TestTelemetryEvent rEvent = null;
            using (bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>()
                         .Subscribe(e => rEvent = e))
            {
                bbMock.Object.Publish(tEvent, handle);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

                rEvent.Should().NotBeNull();
                rEvent.Should().Be(tEvent);
                rEvent.CorrelationVector.Should().Be(bbMock.Object.CorrelationHandles[handle].Vector);
            }
        }

        [Fact, IsUnit]
        public async Task Ensure_LoseOverridesStrictCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };
            var handle = new object();
            var tEvent = new TestTelemetryEvent();

            TestTelemetryEvent rEvent = null;
            using (bbMock.Object.CreateCorrelation())
            using (bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e))
            {
                bbMock.Object.Publish(tEvent, handle);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

                rEvent.Should().NotBeNull();
                rEvent.Should().Be(tEvent);
                rEvent.CorrelationVector.Should().Be(bbMock.Object.CorrelationHandles[handle].Vector);

                bbMock.Object.Handle.Should().NotBeNull();
            }
        }

        [Fact, IsUnit]
        public async Task Test_Publish_EndsTimedEvents()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };
            var tEvent = new BbTimedEvent();

            await Task.Delay(TimeSpan.FromSeconds(2));

            bbMock.Object.Publish(tEvent);

            await Task.Delay(TimeSpan.FromSeconds(3));

            tEvent.ProcessingTime.Should().BeCloseTo(TimeSpan.FromSeconds(2), 1000); // can do a 1sec range here because we have a second 3 second delay
        }
    }

    public class CreateCorrelation
    {
        [Fact, IsUnit]
        public void Test_CreateWithoutOne()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };

            var result = bbMock.Object.CreateCorrelation();

            bbMock.Object.Handle.Should().Be(result);
        }

        /// <remarks>
        /// You can't debug this test, because in debug this will throw instead of the normal behaviour flow.
        /// </remarks>
        [Fact, IsUnit]
        public void Ensure_CreateWithtOneReturnsSame()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };

            BbExceptionEvent errorEvent = null;

            using (BigBrother.InternalStream.OfType<BbExceptionEvent>()
                             .Subscribe(e => errorEvent = e))
            {
                var handle1 = bbMock.Object.CreateCorrelation();
                var handle2 = bbMock.Object.CreateCorrelation();
                handle2.Should().Be(handle1);

                errorEvent.Should().NotBeNull();
                errorEvent.Exception.Should().BeOfType<InvalidOperationException>();
            }
        }
    }

    public class ReleaseCorrelationVectors
    {
        [Fact, IsFakes]
        public void Test_ReleaseHandleNotAlive()
        {
            var now = DateTime.Now.AddMinutes(15); // offset now by 15 minutes, this way we don't need to play around with the internal handle
            var handle = new object();

            BigBrother.Now = () => now;

            var bb = new BigBrother();
            bb.Publish(new TestTelemetryEvent(), handle); // no setup on the subscriptions, so nothing will get published

            bb.ReleaseCorrelationVectors(null);

            bb.CorrelationHandles.Should().BeEmpty();
        }
    }

    public class SetupSubscriptions
    {
        [Fact, IsUnit]
        public async Task Test_Telemetry_IsSubscribed()
        {
            var e = new TestTelemetryEvent();
            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.HandleAiEvent(It.IsAny<BbTelemetryEvent>())).Verifiable();

            bbMock.Object.SetupSubscriptions();
            bbMock.Object.Publish(e);

            await Task.Delay(TimeSpan.FromSeconds(1)); // give the subscription some love

            bbMock.Verify(x => x.HandleAiEvent(e), Times.Once);

            // wipe all internal subscriptions
            BigBrotherExtensions.WipeInternalSubscriptions();
        }

        [Fact, IsUnit]
        public async Task Test_Internal_IsSubscribed()
        {
            var e = new TestTelemetryEvent();
            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.HandleInternalEvent(It.IsAny<BbTelemetryEvent>())).Verifiable();

            bbMock.Object.SetupSubscriptions();
            BigBrother.InternalStream.OnNext(e);

            await Task.Delay(TimeSpan.FromSeconds(1)); // give the subscription some love

            bbMock.Verify(x => x.HandleInternalEvent(e), Times.Once);

            // wipe all internal subscriptions
            BigBrotherExtensions.WipeInternalSubscriptions();
        }
    }

    public class HandleEvent
    {
        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_MetricEvent(bool @internal)
        {
            var telemetry = new BbMetricEvent();

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackEvent(It.IsAny<EventTelemetry>(), @internal)).Verifiable();

            if (@internal)
                bbMock.Object.HandleInternalEvent(telemetry);
            else
                bbMock.Object.HandleAiEvent(telemetry);

            bbMock.Verify();
        }

        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_TimedEvent(bool @internal)
        {
            var telemetry = new BbTimedEvent();

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackEvent(It.IsAny<EventTelemetry>(), @internal)).Verifiable();

            if (@internal)
                bbMock.Object.HandleInternalEvent(telemetry);
            else
                bbMock.Object.HandleAiEvent(telemetry);

            bbMock.Verify();
        }

        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_ExceptionEvent(bool @internal)
        {
            var telemetry = new BbExceptionEvent(new Exception("KABUM"));

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackException(It.IsAny<ExceptionTelemetry>(), @internal))
                  .Callback<ExceptionTelemetry, bool>(
                      (t, b) =>
                      {
                          t.SeverityLevel.Should().Be(SeverityLevel.Error);
                      })
                  .Verifiable();

            if (@internal)
                bbMock.Object.HandleInternalEvent(telemetry);
            else
                bbMock.Object.HandleAiEvent(telemetry);

            bbMock.Verify();
        }
    }

    public class Write
    {
        [Fact, IsUnit]
        public void Test_WriteEvent_WithTraceEvent()
        {
            const string exceptionMessage = "KABUM";
            var completed = Task.Factory.StartNew(
                                    () =>
                                    {
                                        using (var session = new TraceEventSession($"TestSession_{nameof(Test_WriteEvent_WithTraceEvent)}"))
                                        {
                                            session.Source.Dynamic.AddCallbackForProviderEvent(
                                                ErrorEventSource.EventSourceName,
                                                nameof(ErrorEventSource.Tasks.BbExceptionEvent),
                                                e =>
                                                {
                                                    e.PayloadByName("message").Should().Be(exceptionMessage);
                                                    e.PayloadByName("eventPayload").Should().NotBeNull();

                                                    // ReSharper disable once AccessToDisposedClosure
                                                    session.Source?.Dispose();
                                                });

                                            session.EnableProvider(ErrorEventSource.EventSourceName);

                                            Task.Factory.StartNew(() =>
                                            {
                                                Task.Delay(TimeSpan.FromSeconds(3));
                                                BigBrother.Write(new BbExceptionEvent(new Exception(exceptionMessage)));
                                            });

                                            session.Source.Process();
                                        }
                                    })
                                .Wait(TimeSpan.FromSeconds(30));

            completed.Should().BeTrue();
        }
    }

    internal static void BlowUp(string message)
    {
        throw new Exception(message);
    }
}

public class TestTelemetryEvent : BbTelemetryEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }

    public TestTelemetryEvent()
    {
        Id = Guid.NewGuid();
        Description = Lorem.GetSentence();
    }
}

public class TestExceptionEvent : BbExceptionEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }

    public TestExceptionEvent(Exception ex)
        : base(ex)
    {
        Id = Guid.NewGuid();
        Description = Lorem.GetSentence();
    }
}

public class TestTimedEvent : BbTimedEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }

    public TestTimedEvent()
    {
        Id = Guid.NewGuid();
        Description = Lorem.GetSentence();
    }
}

public static class BigBrotherExtensions
{
    public static void WipeInternalSubscriptions()
    {
        foreach (var sub in BigBrother.InternalSubscriptions.Values)
        {
            sub.Dispose();
        }
        BigBrother.InternalSubscriptions.Clear();
    }
}