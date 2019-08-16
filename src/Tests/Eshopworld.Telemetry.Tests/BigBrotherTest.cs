using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
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
            IBigBrother bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(new TestTelemetryEvent());
            bb.Flush();
        }

        [Fact, IsDev]
        public void EntryPoint_PushException()
        {
            const string message = "KABOOM!!!";
            IBigBrother bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            try
            {
                BlowUp(message);
            }
            catch (Exception ex)
            {
                bb.Publish(ex.ToExceptionEvent());
                bb.Flush();
            }
        }

        [Fact, IsDev]
        public void EntryPoint_PushTimed()
        {
            IBigBrother bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(new TestTimedEvent());
            bb.Flush();
        }

        [Fact, IsDev]
        public void EntryPoint_PushAnonymous()
        {
            IBigBrother bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(new
            {
                SomeStuff = Lorem.GetSentence(),
                SomeMoreStuff = Lorem.GetSentence(),
                AndEvenMoreStuff = Lorem.GetSentence()
            });

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
            }
        }

        [Fact, IsUnit]
        public async Task Test_Publish_EndsTimedEvents()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true };
            var tEvent = new TimedTelemetryEvent();

            await Task.Delay(TimeSpan.FromSeconds(2));

            bbMock.Object.Publish(tEvent);

            await Task.Delay(TimeSpan.FromSeconds(3));

            tEvent.ProcessingTime.Should().BeCloseTo(TimeSpan.FromSeconds(2), 1000); // can do a 1sec range here because we have a second 3 second delay
        }
    }

    public class SetupSubscriptions
    {
        [Fact, IsUnit]
        public async Task Test_Telemetry_IsSubscribed()
        {
            var e = new TestTelemetryEvent();
            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.HandleAiEvent(It.IsAny<TelemetryEvent>())).Verifiable();

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
            bbMock.Setup(x => x.HandleInternalEvent(It.IsAny<TelemetryEvent>())).Verifiable();

            bbMock.Object.SetupSubscriptions();
            BigBrother.InternalStream.OnNext(e);

            await Task.Delay(TimeSpan.FromSeconds(1)); // give the subscription some love

            bbMock.Verify(x => x.HandleInternalEvent(e), Times.Once);

            // wipe all internal subscriptions
            BigBrotherExtensions.WipeInternalSubscriptions();
        }

        [Fact, IsDev]
        public async Task Test_HighContention()
        {
            var tasks = new List<Task>();
            for (var x = 0; x < 10; x++)
            {
                tasks.Add(Task.Run(()=> new BigBrother("blah", "blah")));
            }

            //this will blow up in V2
            await Task.WhenAll(tasks.ToArray());
        }
    }

    public class HandleEvent
    {
        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_MetricEvent(bool isInternal)
        {
            var telemetry = new MetricTelemetryEvent();

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackEvent(It.IsAny<EventTelemetry>(), isInternal)).Verifiable();

            if (isInternal)
                bbMock.Object.HandleInternalEvent(telemetry);
            else
                bbMock.Object.HandleAiEvent(telemetry);

            bbMock.Verify();
        }

        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_TimedEvent(bool isInternal)
        {
            var telemetry = new TimedTelemetryEvent();

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackEvent(It.IsAny<EventTelemetry>(), isInternal)).Verifiable();

            if (isInternal)
                bbMock.Object.HandleInternalEvent(telemetry);
            else
                bbMock.Object.HandleAiEvent(telemetry);

            bbMock.Verify();
        }

        [Theory, IsUnit]
        [InlineData(true)]
        [InlineData(false)]
        public void Test_With_ExceptionEvent(bool isInternal)
        {
            var telemetry = new ExceptionEvent(new Exception("KABUM"));

            var bbMock = new Mock<BigBrother> { CallBase = true };
            bbMock.Setup(x => x.TrackException(It.IsAny<ExceptionTelemetry>(), isInternal))
                  .Callback<ExceptionTelemetry, bool>(
                      (t, b) =>
                      {
                          t.SeverityLevel.Should().Be(SeverityLevel.Error);
                      })
                  .Verifiable();

            if (isInternal)
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
                                                nameof(ErrorEventSource.Tasks.ExceptionEvent),
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
                                                BigBrother.Write(new ExceptionEvent(new Exception(exceptionMessage)));
                                            });

                                            session.Source.Process();
                                        }
                                    })
                                .Wait(TimeSpan.FromSeconds(30));

            completed.Should().BeTrue();
        }

        [Fact, IsUnit]
        public async Task Test_With_ExtendedExceptionEvent()
        {
            var telemetry = new ExtendedTestException(new Exception("blah")) { ErrorCode = HttpStatusCode.Accepted , ClientId = "clientId"};

            var channel = new Mock<ITelemetryChannel>();

            channel.Setup(c => c.Send(It.Is<ExceptionTelemetry>(i => 
                i.Properties["ErrorCode"]=="202" && i.Properties["Timestamp"]!=null && i.Properties["ClientId"]=="clientId")))
                .Verifiable();

            var telClient = new TelemetryClient(new TelemetryConfiguration("blah", channel.Object));
            
            using (var bb = new BigBrother(telClient, "blah"))
            {
                bb.Publish(telemetry.ToExceptionEvent());
                bb.Flush();

                await Task.Delay(TimeSpan.FromSeconds(1));
                channel.Verify();
            }
        }
    }

    internal static void BlowUp(string message)
    {
        throw new Exception(message);
    }
}

public class ExtendedTestException : Exception
{
    public ExtendedTestException(Exception e) : base("blah", e)
    { }

    public string ClientId { get; set; }

    public HttpStatusCode ErrorCode { get; set; }

    public DateTime Timestamp { get; set; }
}

public class TestTelemetryEvent : TelemetryEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }

    public TestTelemetryEvent()
    {
        Id = Guid.NewGuid();
        Description = Lorem.GetSentence();
    }
}

public class TestExceptionEvent : ExceptionEvent
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

public class TestTimedEvent : TimedTelemetryEvent
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