using System;
using DevOpsFlex.Telemetry;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherTest
{
    [Fact, Trait("Category", "Dev")]
    public void EntryPoint()
    {
        var bb = new BigBrother("", "").DeveloperMode();

        bb.Publish(
            new TestTelemetryEvent
            {
                Id = Guid.NewGuid(),
                Description = "some random piece of text"
            });

        bb.Flush();
    }
}

public class TestTelemetryEvent : BbTelemetryEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }
}
