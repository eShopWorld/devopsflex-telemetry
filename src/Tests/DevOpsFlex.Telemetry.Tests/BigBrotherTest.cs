using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Telemetry.Tests;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherTest
{
    internal static readonly string DevKey = Environment.GetEnvironmentVariable("devai", EnvironmentVariableTarget.User);

    public class Dev
    {
        [Fact, IsDev]
        public void EntryPoint_PushTelemetry()
        {
            var bb = new BigBrother(DevKey, DevKey).DeveloperMode();

            bb.Publish(
                new TestTelemetryEvent
                {
                    Id = Guid.NewGuid(),
                    Description = "some random piece of text"
                });

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
    }

    public class CreateCorrelation
    {
        [Fact, IsUnit]
        public void Test_CreateWithoutOne()
        {
            var bbMock = new Mock<BigBrother> {CallBase = true}.WithoutSetup();

            var result = bbMock.Object.CreateCorrelation();

            bbMock.Object.Handle.Should().Be(result);
        }

        /// <remarks>
        /// You can't debug this test, because in debug this will throw instead of the normal behaviour flow.
        /// </remarks>
        [Fact, IsUnit]
        public void Ensure_CreateWithtOneReturnsSame()
        {
            var bbMock = new Mock<BigBrother> {CallBase = true}.WithoutSetup();

            BbExceptionEvent errorEvent = null;
            BigBrother.InternalStream.OfType<BbExceptionEvent>()
                      .Subscribe(
                          e =>
                          {
                              errorEvent = e;
                          });

            var handle1 = bbMock.Object.CreateCorrelation();
            var handle2 = bbMock.Object.CreateCorrelation();
            handle2.Should().Be(handle1);

            errorEvent.Should().NotBeNull();
            errorEvent.Exception.Should().BeOfType<InvalidOperationException>();
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
}
