using System;
using System.Fakes;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Telemetry.Fakes;
using DevOpsFlex.Telemetry.Tests;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Microsoft.QualityTools.Testing.Fakes;
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

    public class Publish
    {
        [Fact, IsUnit]
        public async Task Test_PublishWithoutCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();
            var tEvent = new TestTelemetryEvent();

            TestTelemetryEvent rEvent = null;
            bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e);

            bbMock.Object.Publish(tEvent);

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

            rEvent.Should().NotBeNull();
            rEvent.Should().Be(tEvent);
            rEvent.CorrelationVector.Should().BeNull();
        }

        [Fact, IsUnit]
        public async Task Test_PublishUnderStrictCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();

            using (bbMock.Object.CreateCorrelation())
            {
                var tEvent = new TestTelemetryEvent();

                TestTelemetryEvent rEvent = null;
                bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e);

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
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();
            var handle = new object();
            var tEvent = new TestTelemetryEvent();

            TestTelemetryEvent rEvent = null;
            bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>()
                  .Subscribe(e => rEvent = e);

            bbMock.Object.Publish(tEvent, handle);

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

            rEvent.Should().NotBeNull();
            rEvent.Should().Be(tEvent);
            rEvent.CorrelationVector.Should().Be(bbMock.Object.CorrelationHandles[handle].Vector);
        }

        [Fact, IsUnit]
        public async Task Ensure_LoseOverridesStrictCorrelation()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();
            var handle = new object();
            var tEvent = new TestTelemetryEvent();

            using (bbMock.Object.CreateCorrelation())
            {
                TestTelemetryEvent rEvent = null;
                bbMock.Object.TelemetryStream.OfType<TestTelemetryEvent>().Subscribe(e => rEvent = e);

                bbMock.Object.Publish(tEvent, handle);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait a bit to ensure the subscription gets love

                rEvent.Should().NotBeNull();
                rEvent.Should().Be(tEvent);
                rEvent.CorrelationVector.Should().Be(bbMock.Object.CorrelationHandles[handle].Vector);

                bbMock.Object.Handle.Should().NotBeNull();
            }
        }
    }

    public class CreateCorrelation
    {
        [Fact, IsUnit]
        public void Test_CreateWithoutOne()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();

            var result = bbMock.Object.CreateCorrelation();

            bbMock.Object.Handle.Should().Be(result);
        }

        /// <remarks>
        /// You can't debug this test, because in debug this will throw instead of the normal behaviour flow.
        /// </remarks>
        [Fact, IsUnit]
        public void Ensure_CreateWithtOneReturnsSame()
        {
            var bbMock = new Mock<BigBrother> { CallBase = true }.WithoutSetup();

            BbExceptionEvent errorEvent = null;
            BigBrother.InternalStream.OfType<BbExceptionEvent>()
                      .Subscribe(e => errorEvent = e);

            var handle1 = bbMock.Object.CreateCorrelation();
            var handle2 = bbMock.Object.CreateCorrelation();
            handle2.Should().Be(handle1);

            errorEvent.Should().NotBeNull();
            errorEvent.Exception.Should().BeOfType<InvalidOperationException>();
        }
    }

    public class ReleaseCorrelationVectors
    {
        [Fact, IsUnit]
        public void Foo()
        {
            using (ShimsContext.Create())
            {
                var now = DateTime.Now.AddMinutes(15); // offset now by 15 minutes, this way we don't need to play around with the internal handle
                var handle = new object();

                // ReSharper disable once AccessToModifiedClosure
                ShimDateTime.NowGet = () => now;

                ShimBigBrother.AllInstances.SetupSubscriptions = _ => { };
                ShimBigBrother.AllInstances.SetupTelemetryClientStringString = (_, __, ___) => { };

                var bb = new BigBrother();
                bb.Publish(new TestTelemetryEvent(), handle); // no setup on the subscriptions, so nothing will get published

                bb.ReleaseCorrelationVectors(null);

                bb.CorrelationHandles.Should().BeEmpty();
            }
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
