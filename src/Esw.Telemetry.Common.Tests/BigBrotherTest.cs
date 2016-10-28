using System;
using Esw.Telemetry.Common;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherTest
{
    [Fact, Trait("Category", "Dev")]
    public void EntryPoint()
    {
        var bb = new BigBrother("a5f23ef9-72fa-47e7-b6a8-89a064eb9c31", "a5f23ef9-72fa-47e7-b6a8-89a064eb9c31").DeveloperMode();

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
