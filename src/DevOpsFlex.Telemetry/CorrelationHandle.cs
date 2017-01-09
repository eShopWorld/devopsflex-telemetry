namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents a lose correlation handle that binds together a time frame for keep alive and a vector.
    /// </summary>
    public class CorrelationHandle
    {
        internal readonly TimeSpan KeepAlive;
        internal DateTime LastTouch;

        /// <summary>
        /// Gets the Vector associated with this handle.
        /// </summary>
        public string Vector { get; }

        /// <summary>
        /// Returns true if this handle should be kept alive or not based on the keep alive time frame.
        /// </summary>
        /// <param name="now">The DateTime.Now that is passed in to speed up enumerations.</param>
        /// <returns>True if the handle should be kept alive, false otherwise.</returns>
        public bool IsAlive(DateTime now) => LastTouch.Add(KeepAlive) > now;

        /// <summary>
        /// Initializes a new instance of <see cref="CorrelationHandle"/>.
        /// </summary>
        public CorrelationHandle()
        {
            Vector = Guid.NewGuid().ToBase64();
            LastTouch = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CorrelationHandle"/>.
        /// </summary>
        /// <param name="keepAlive">The number of minutes to keep this handle alive.</param>
        public CorrelationHandle(TimeSpan keepAlive)
            :this()
        {
            KeepAlive = keepAlive;
        }

        /// <summary>
        /// Touches (Refreshes) the keep alive timer on the handle.
        /// </summary>
        public void Touch()
        {
            LastTouch = DateTime.Now;
        }

    }

    /// <summary>
    /// Contains internal extension methods usefull for the <see cref="CorrelationHandle"/>.
    /// </summary>
    internal static class CorrelationVectorExtensions
    {
        /// <summary>
        /// Converts a <see cref="Guid"/> to a Base64 encoded string. This will reduce the <see cref="Guid"/> size to about 20% without using compression.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> that we want to convert.</param>
        /// <returns>The Base64 encoded string.</returns>
        internal static string ToBase64(this Guid guid)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(guid.ToString()));
        }
    }
}
