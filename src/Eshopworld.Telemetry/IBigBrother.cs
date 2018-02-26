namespace Eshopworld.Telemetry
{
    using System;
    using DevOpsFlex.Core;
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
        /// <param name="correlation">The correlation handle if you want to correlate events</param>
        void Publish([NotNull]BbEvent bbEvent, object correlation = null);

        /// <summary>
        /// Forces the telemetry channel to be in developer mode, where it will instantly push
        /// telemetry to the Application Insights account.
        /// </summary>
        [NotNull] IBigBrother DeveloperMode();

        /// <summary>
        /// Creates a strict correlation handle for synchronous correlation.
        /// </summary>
        /// <returns>The correlation handle as an <see cref="IDisposable"/>.</returns>
        [NotNull] IDisposable CreateCorrelation();

        /// <summary>
        /// Gets the associated <see cref="string"/> Vector to the given handle.
        /// </summary>
        /// <param name="handle">The handle used to correlate events.</param>
        /// <returns>The correlation Vector as a <see cref="string"/>, or null if your handle can't be found.</returns>
        [CanBeNull] string GetCorrelationVector([NotNull] object handle);

        /// <summary>
        /// Flush out all telemetry clients, both the external and the internal one.
        /// </summary>
        /// <remarks>
        /// There is internal telemetry associated with calling this method to prevent bad usage.
        /// </remarks>
        void Flush();

        /// <summary>
        /// Sets the ammount of minutes to keep a lose correlation object reference alive.
        /// </summary>
        /// <param name="span">The <see cref="TimeSpan"/> to keep a lose correlation handle alive.</param>
        void SetCorrelationKeepAlive(TimeSpan span);
    }
}
