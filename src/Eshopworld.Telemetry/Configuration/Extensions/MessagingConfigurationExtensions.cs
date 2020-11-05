using System;
using Eshopworld.Core;

namespace Eshopworld.Telemetry.Configuration.Extensions
{
    public static class MessagingConfigurationExtensions
    {
        /// <summary>
        /// Sets up telemetry to use topics to stream some <see cref="DomainEvent"/> types.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <param name="publisher">The event publisher we are using to send events onto topics.</param>
        public static void PublishEventsToTopics(this IBigBrother bb, IPublishEvents publisher)
        {
            var bbImpl = bb as BigBrother ?? throw new InvalidOperationException($"Couldn't cast this instance of {nameof(IBigBrother)} to a concrete implementation of {nameof(BigBrother)}");
            bbImpl.TopicPublisher = publisher;
        }
    }
}