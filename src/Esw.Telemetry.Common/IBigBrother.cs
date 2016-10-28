namespace Esw.Telemetry.Common
{
    public interface IBigBrother
    {
        /// <summary>
        /// Publishes a <see cref="BbEvent"/> through the pipeline.
        /// </summary>
        /// <param name="bbEvent">The event that we want to publish.</param>
        void Publish(BbEvent bbEvent);

        /// <summary>
        /// Forces the telemetry channel to be in developer mode, where it will instantly push
        /// telemetry to the Application Insights account.
        /// </summary>
        IBigBrother DeveloperMode();

        void Flush();
    }
}
