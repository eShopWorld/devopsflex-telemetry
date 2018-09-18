namespace Eshopworld.Telemetry
{
    using System;
    using Core;

    public static class BbMessagingConfiguration
    {
        /// <summary>
        /// Sets up telemetry to use topics to stream some <see cref="DomainEvent"/> types.
        /// </summary>
        /// <param name="bb">The <see cref="BigBrother"/> instance we are configuring.</param>
        /// <param name="publisher">The event publisher we are using to send events onto topics.</param>
        /// <returns></returns>
        public static IConfigureMessaging PublishEventsToTopics(this BigBrother bb, IPublishEvents publisher)
        {
            return new MessagingConfiguration(bb, publisher);
        }

        /// <summary>
        /// Configures messaging publishing to topics for a specific event type.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of event we want to publish to topics also.</typeparam>
        /// <param name="messagingConfiguration">The fluent api configuration instance we are extending.</param>
        /// <returns>Itself - the configuration instance.</returns>
        public static IConfigureMessaging For<T>(this IConfigureMessaging messagingConfiguration)
        {
            ((MessagingConfiguration)messagingConfiguration).AddType(typeof(T));
            return messagingConfiguration;
        }
    }

    /// <summary>
    /// Fluent implementation of <see cref="IConfigureMessaging"/> used to pass on a configuration construct to add more types to topics publishing.
    /// </summary>
    public class MessagingConfiguration : IConfigureMessaging
    {
        private readonly BigBrother _bbInstance;

        internal MessagingConfiguration(BigBrother bbInstance, IPublishEvents publisher)
        {
            _bbInstance = bbInstance;
            _bbInstance.TopicPublisher = publisher;
        }

        internal void AddType(Type type)
        {
            _bbInstance.PublishTypeSet.Add(type);
        }
    }

    /// <summary>
    /// Fluent interface to pass on a messaging configuration premise.
    /// </summary>
    public interface IConfigureMessaging
    {
    }
}