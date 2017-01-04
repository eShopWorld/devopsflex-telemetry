namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents a lose correlation handle that binds together a time frame for keep alive and a vector.
    /// </summary>
    public class CorrelationHandle
    {
        private const int DefaultKeepAliveMinutes = 10;

        private readonly int _keepAliveMinutes;
        private DateTime _lastTouch;

        /// <summary>
        /// Gets the Vector associated with this handle.
        /// </summary>
        public string Vector { get; }

        /// <summary>
        /// Returns true if this handle should be kept alive or not based on the keep alive time frame.
        /// </summary>
        /// <param name="now">The DateTime.Now that is passed in to speed up enumerations.</param>
        /// <returns>True if the handle should be kept alive, false otherwise.</returns>
        public bool IsAlive(DateTime now) => _lastTouch.AddMinutes(_keepAliveMinutes) < now;

        /// <summary>
        /// Initializes a new instance of <see cref="CorrelationHandle"/>.
        /// </summary>
        public CorrelationHandle()
        {
            Vector = Guid.NewGuid().ToBase64();
            _lastTouch = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CorrelationHandle"/>.
        /// </summary>
        /// <param name="keepAliveMinutes">The number of minutes to keep this handle alive.</param>
        public CorrelationHandle(int keepAliveMinutes)
            :this()
        {
            _keepAliveMinutes = keepAliveMinutes;
        }

        /// <summary>
        /// Touches (Refreshes) the keep alive timer on the handle.
        /// </summary>
        public void Touch()
        {
            _lastTouch = DateTime.Now;
        }

    }

    internal static class CorrelationVectorExtensions
    {
        internal static string ToBase64(this Guid guid)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(guid.ToString()));
        }
    }
}
