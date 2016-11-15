using System;
using DevOpsFlex.Telemetry;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherTest
{
    private readonly string _devKey = Environment.GetEnvironmentVariable("devai", EnvironmentVariableTarget.User);

    [Fact, Trait("Category", "Dev")]
    public void EntryPoint_PushTelemetry()
    {
        var bb = new BigBrother(_devKey, _devKey).DeveloperMode();

        bb.Publish(
            new TestTelemetryEvent
            {
                Id = Guid.NewGuid(),
                Description = "some random piece of text"
            });

        bb.Flush();
    }

    [Fact, Trait("Category", "Dev")]
    public void EntryPoint_PushException()
    {
        const string message = "KABOOM!!!";
        var bb = new BigBrother(_devKey, _devKey).DeveloperMode();

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

    private static void BlowUp(string message)
    {
        throw new Exception(message);
    }
}

public class TestTelemetryEvent : BbTelemetryEvent
{
    public Guid Id { get; set; }

    public string Description { get; set; }
}
