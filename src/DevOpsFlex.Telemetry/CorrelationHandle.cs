namespace DevOpsFlex.Telemetry
{
    using System;

    public class CorrelationHandle : IDisposable
    {
        internal BigBrother Bb;
        internal Guid Id = Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of <see cref="CorrelationHandle"/>.
        /// </summary>
        /// <param name="bb">The parent instance of <see cref="BigBrother"/>.</param>
        public CorrelationHandle(BigBrother bb)
        {
            Bb = bb;
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
