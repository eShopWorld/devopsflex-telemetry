using Eshopworld.Core;

namespace Eshopworld.Telemetry.Configuration
{
    /// <summary>
    /// Configures BigBrother to send domain events to the specified publisher.
    /// </summary>
    class BigBrotherEventsPublisherInitializer : IBigBrotherInitializer
    {
        private readonly IPublishEvents _publishEvents;

        /// <summary>
        /// Creates and instance of <see cref="BigBrotherEventsPublisherInitializer"/>.
        /// </summary>
        /// <param name="publishEvents">The domain event's publisher.</param>
        public BigBrotherEventsPublisherInitializer(IPublishEvents publishEvents = null)
        {
            _publishEvents = publishEvents;
        }

        /// <summary>
        /// Configures how the <see cref="BigBrother"/> instance processes domain events.
        /// </summary>
        /// <param name="bigBrother">The BigBrother instance to configure.</param>
        public void Initialize(BigBrother bigBrother)
        {
            if (_publishEvents != null)
                bigBrother.PublishEventsToTopics(_publishEvents);
        }
    }
}
