namespace Eshopworld.Telemetry
{
    using Core;

    public static class MessagingConfigurationExtensions
    {
        /// <summary>
        /// Sets up telemetry to use topics to stream some <see cref="DomainEvent"/> types.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <param name="publisher">The event publisher we are using to send events onto topics.</param>
        public static void PublishEventsToTopics(this BigBrother bb, IPublishEvents publisher)
        {
            bb.TopicPublisher = publisher;
        }
    }
}