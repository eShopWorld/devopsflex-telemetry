using Eshopworld.Core;

namespace Eshopworld.Telemetry.DependencyInjection
{
    class BigBrotherEventsPublisherInitializer : IBigBrotherInitializer
    {
        private readonly IPublishEvents _publishEvents;

        public BigBrotherEventsPublisherInitializer(IPublishEvents publishEvents = null)
        {
            _publishEvents = publishEvents;
        }

        public void Initialize(BigBrother bigBrother)
        {
            if (_publishEvents != null)
                bigBrother.PublishEventsToTopics(_publishEvents);
        }
    }
}
