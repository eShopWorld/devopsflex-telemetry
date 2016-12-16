namespace DevOpsFlex.Telemetry
{
    using JetBrains.Annotations;

    /// <summary>
    /// Contract that provides a way to publish telemetry events for instrumentation.
    /// </summary>
    public interface IBigBrother
    {
        /// <summary>
        /// Publishes a <see cref="BbEvent"/> through the pipeline.
        /// </summary>
        /// <param name="bbEvent">The event that we want to publish.</param>
        void Publish([NotNull]BbEvent bbEvent);

        /// <summary>
        /// Forces the telemetry channel to be in developer mode, where it will instantly push
        /// telemetry to the Application Insights account.
        /// </summary>
        IBigBrother DeveloperMode();

        /// <summary>
        /// Flush out all telemetry clients, both the external and the internal one.
        /// </summary>
        /// <remarks>
        /// There is internal telemetry associated with calling this method to prevent bad usage.
        /// </remarks>
        void Flush();
    }
}
