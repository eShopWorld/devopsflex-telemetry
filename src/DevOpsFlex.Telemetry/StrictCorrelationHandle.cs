namespace DevOpsFlex.Telemetry
{
    using System;

    /// <summary>
    /// Used to create strict correlation handles that while they last will correlate all events
    /// published. To terminate them call .Dispose() or use a using block.
    /// </summary>
    public class StrictCorrelationHandle : IDisposable
    {
        internal BigBrother Bb;
        internal string Vector = Guid.NewGuid().ToBase64();

        /// <summary>
        /// Initializes a new instance of <see cref="StrictCorrelationHandle"/>.
        /// </summary>
        /// <param name="bb">The parent instance of <see cref="BigBrother"/>.</param>
        public StrictCorrelationHandle(BigBrother bb)
        {
            Bb = bb;
            Bb.Handle = this;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Bb.Handle = null;
            Bb = null;
        }
    }
}
