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
    }

    internal static void BlowUp(string message)
    {
        throw new Exception(message);
    }
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