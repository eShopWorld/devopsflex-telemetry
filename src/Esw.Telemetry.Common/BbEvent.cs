namespace Esw.Telemetry.Common
{
    /// <summary>
    /// Base class for all events pushed by <see cref="BigBrother"/>. Can't be inherited directly but is base
    /// to all the classes that you can inherit from.
    /// </summary>
    public class BbEvent
    {
        /// <summary>
        /// Internally initializes an instance of <see cref="BbEvent"/>.
        /// </summary>
        internal BbEvent() { }
    }
}
