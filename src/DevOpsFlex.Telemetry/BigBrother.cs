namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using InternalEvents;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Deals with everything that's public in telemetry.
    /// This is the main entry point in the <see cref="DevOpsFlex.Telemetry"/> API.
    /// </summary>
    public class BigBrother : IBigBrother, IDisposable
    {
        /// <summary>
        /// The unique internal <see cref="TelemetryClient"/> used to stream events to the AI account.
        /// </summary>
        private static readonly TelemetryClient InternalClient;

        /// <summary>
        /// The internal telemetry stream, used by this package to report errors and usage to an internal AI account.
        /// </summary>
        private static readonly Subject<BbEvent> InternalStream = new Subject<BbEvent>();

        /// <summary>
        /// Static initialization of static resources in <see cref="BigBrother"/> instances.
        /// </summary>
        static BigBrother()
        {
            InternalClient = new TelemetryClient();
        }

        private readonly TelemetryClient _telemetryClient;
        private readonly IDisposable _internalSubscription;
        private readonly Dictionary<Type, IDisposable> _telemetrySubscriptions = new Dictionary<Type, IDisposable>();
        private readonly Dictionary<object, CorrelationHandle> _correlationHandles = new Dictionary<object, CorrelationHandle>();
        private readonly Timer _correlationReleaseTimer;

        /// <summary>
        /// The main event stream that's exposed publicly (yea ... subjects are bad ... I'll redesign when and if time allows).
        /// </summary>
        private readonly Subject<BbEvent> _telemetryStream = new Subject<BbEvent>();

        private int _keepAliveMinutes = 10;

        /// <summary>
        /// The current strict correlation handle.
        /// </summary>
        internal StrictCorrelationHandle Handle;

        /// <summary>
        /// Just here for internal easy Mocking of the concrete class.
        /// </summary>
        internal BigBrother() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BigBrother"/>.
        /// This constructor does a bit of work, so if you're mocking this, mock the <see cref="IBigBrother"/> contract instead.
        /// </summary>
        /// <param name="aiKey">The application's Application Insights instrumentation key.</param>
        /// <param name="internalKey">The devops internal telemetry Application Insights instrumentation key.</param>
        public BigBrother([NotNull]string aiKey, [NotNull]string internalKey)
        {
            _correlationReleaseTimer = new Timer(ReleaseCorrelationVectors, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            _telemetryClient = new TelemetryClient
            {
                InstrumentationKey = aiKey
            };

            InternalClient.InstrumentationKey = internalKey;

            _telemetrySubscriptions.Add(
                typeof(BbTelemetryEvent),
                _telemetryStream.OfType<BbTelemetryEvent>()
                                .Subscribe(e => _telemetryClient.TrackEvent(e.ToTelemetry())));

            _telemetrySubscriptions.Add(
                typeof(BbExceptionEvent),
                _telemetryStream.OfType<BbExceptionEvent>()
                                .Subscribe(
                                    e =>
                                    {
                                        var tEvent = e.ToTelemetry();
                                        if (tEvent == null) return;

                                        tEvent.SeverityLevel = SeverityLevel.Error;

                                        _telemetryClient.TrackException(tEvent);
                                    }));

            _internalSubscription = InternalStream.OfType<BbTelemetryEvent>()
                                                  .Subscribe(e => _telemetryClient.TrackEvent(e.ToTelemetry()));

            _internalSubscription = InternalStream.OfType<BbExceptionEvent>()
                                                  .Subscribe(
                                                      e =>
                                                      {
                                                          var tEvent = e.ToTelemetry();
                                                          if (tEvent == null) return;

                                                          tEvent.SeverityLevel = SeverityLevel.Warning;

                                                          _telemetryClient.TrackException(tEvent);
                                                      });
        }

        /// <summary>
        /// Publishes a <see cref="BbEvent"/> through the pipeline.
        /// </summary>
        /// <param name="bbEvent">The event that we want to publish.</param>
        /// <param name="correlation">The correlation handle if you want to correlate events</param>
        public void Publish(BbEvent bbEvent, object correlation = null)
        {
            if (correlation != null) // lose > strict, so we override strict if a lose is passed in
            {
                if (_correlationHandles.ContainsKey(correlation))
                {
                    bbEvent.CorrelationVector = _correlationHandles[correlation].Vector;
                    _correlationHandles[correlation].Touch();
                }
                else
                {
                    var handle = new CorrelationHandle(_keepAliveMinutes);
                    _correlationHandles.Add(correlation, handle);
                    bbEvent.CorrelationVector = handle.Vector;
                }
            }
            else if (Handle != null)
            {
                bbEvent.CorrelationVector = Handle.Vector;
            }

            _telemetryStream.OnNext(bbEvent);
        }

        /// <summary>
        /// Forces the telemetry channel to be in developer mode, where it will instantly push
        /// telemetry to the Application Insights account.
        /// </summary>
        public IBigBrother DeveloperMode()
        {
#if DEBUG
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
#endif
            return this;
        }

        /// <summary>
        /// Creates a strict correlation handle for synchronous correlation.
        /// </summary>
        /// <returns>The correlation handle as an <see cref="IDisposable"/>.</returns>
        public IDisposable CreateCorrelation()
        {
            if (Handle == null) return new StrictCorrelationHandle(this);

            var ex = new InvalidOperationException("You\'re trying to create a second correlation handle while one is active. Use lose correlation instead if you\'re trying to correlate work in parallel with different correlations.");
#if DEBUG
            if (Debugger.IsAttached)
            {
                throw ex;
            }
#endif
            PublishError(ex);
            return Handle;
        }

        /// <summary>
        /// Flush out all telemetry clients, both the external and the internal one.
        /// </summary>
        /// <remarks>
        /// There is internal telemetry associated with calling this method to prevent bad usage.
        /// </remarks>
        public void Flush()
        {
            InternalStream.OnNext(new FlushEvent()); // You're not guaranteed to flush this event
            _telemetryClient.Flush();
            InternalClient.Flush();
        }

        /// <summary>
        /// Sets the ammount of minutes to keep a lose correlation object reference alive.
        /// </summary>
        /// <param name="minutes">The number of minutes to keep a lose correlation handle alive.</param>
        public void SetCorrelationKeepAlive(int minutes)
        {
            _keepAliveMinutes = minutes;
        }

        /// <summary>
        /// Used internal by BigBrother to publish usage exceptions to a special
        /// Application Insights account.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that we want to publish.</param>
        internal static void PublishError(Exception ex)
        {
            InternalStream.OnNext(ex.ToBbEvent());
        }

        /// <summary>
        /// Does a periodic release of old correlation vector object references.
        /// </summary>
        /// <param name="state"></param>
        internal void ReleaseCorrelationVectors(object state)
        {
            var now = DateTime.Now; // Do DateTime.Now once per tick to speed up the release->collect pass.

            foreach (var handle in _correlationHandles.Where(h => h.Value.IsAlive(now)))
            {
                _correlationHandles.Remove(handle.Key);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _correlationReleaseTimer?.Dispose();
            _internalSubscription?.Dispose();

            foreach (var key in _telemetrySubscriptions.Keys)
            {
                _telemetrySubscriptions[key]?.Dispose();
            }
        }
    }
}
