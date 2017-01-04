using System;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once CheckNamespace
public class BbExceptionEventTest
{
    public class ToTelemetry
    {
        [Fact, IsUnit]
        public void Ensure_ExceptionIsntSerializedToProperties()
        {
            const string message = "BOOM";

            var tEvent = new TestExceptionEvent
            {
                Exception = new Exception(message),
                Message = message
            }.ToTelemetry();

            tEvent.Should().NotBeNull();
            tEvent?.Properties.Should().HaveCount(1);
            tEvent?.Properties.Should().NotContainKey(nameof(TestExceptionEvent.Exception));
            tEvent?.Properties.Should().ContainKey(nameof(TestExceptionEvent.Message));
            tEvent?.Properties[nameof(TestExceptionEvent.Message)].Should().Be(message);
        }
    }
}

public class TestExceptionEvent : BbExceptionEvent
{
    public string Message { get; set; }
}