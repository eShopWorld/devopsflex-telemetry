using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Telemetry.Configuration.Extensions;
using Eshopworld.Tests.Core;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class MessagingConfigurationExtensionsTest
{
    [Fact, IsUnit]
    public void Test_Publish_UsesTopics_WhenSetup()
    {
        var bb = BigBrother.CreateDefault("", "");
        bb.TelemetrySubscriptions.Clear(); // disable normal telemetry
        var dEvent = new TestDomainEvent();

        var mPublisher = new Mock<IPublishEvents>();
        mPublisher.Setup(x => x.Publish(It.IsAny<TelemetryEvent>())).Returns(Task.CompletedTask);
        bb.PublishEventsToTopics(mPublisher.Object);

        bb.Publish(dEvent);

        mPublisher.VerifyAll();
    }

    [Fact, IsUnit]
    public void Test_Publish_NoTopics_WhenNotSetup()
    {
        var bb = BigBrother.CreateDefault("", "");
        bb.TelemetrySubscriptions.Clear(); // disable normal telemetry
        var dEvent = new TestDomainEvent();

        var mPublisher = new Mock<IPublishEvents>();

        bb.Publish(dEvent);

        mPublisher.Verify(x => x.Publish(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_Publish_WontSendNonDomainEvents_ToTopics()
    {
        var bb = BigBrother.CreateDefault("", "");
        bb.TelemetrySubscriptions.Clear(); // disable normal telemetry
        var dEvent = new TestTimedEvent();

        var mPublisher = new Mock<IPublishEvents>();
        bb.PublishEventsToTopics(mPublisher.Object);

        bb.Publish(dEvent);

        mPublisher.Verify(x => x.Publish(It.IsAny<TelemetryEvent>()), Times.Never);
    }
}

public class TestDomainEvent : DomainEvent
{
}