namespace Esw.Telemetry.Common
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using System.Reactive.Linq;
    using InternalEvents;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Deals with everything that's public in telemetry.
    /// This is the main entry point in the <see cref="Esw.Telemetry.Common"/> API.
    /// </summary>
    public class BigBrother : IBigBrother, IDisposable
    {
        private static readonly TelemetryClient InternalClient;

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

        /// <summary>
        /// The main event stream that's exposed publicly (yea ... subjects are bad ... I'll redesign when and if time allows).
        /// </summary>
        private readonly Subject<BbEvent> _telemetryStream = new Subject<BbEvent>();

        /// <summary>
        /// The internal telemetry stream, used by this package to report errors and usage to an internal AI account.
        /// </summary>
        private static readonly Subject<BbEvent> InternalStream = new Subject<BbEvent>();

        /// <summary>
        /// Initializes a new instance of <see cref="BigBrother"/>.
        /// This constructor does a bit of work, so if you're mocking this, mock the <see cref="IBigBrother"/> contract instead.
        /// </summary>
        /// <param name="aiKey">The application's Application Insights instrumentation key.</param>
        /// <param name="internalKey">The devops internal telemetry Application Insights instrumentation key.</param>
        public BigBrother([NotNull]string aiKey, [NotNull]string internalKey)
        {
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
                                        tEvent.HandledAt = ExceptionHandledAt.UserCode;

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
                                                          tEvent.HandledAt = ExceptionHandledAt.Platform;

                                                          _telemetryClient.TrackException(tEvent);
                                                      });
        }

        /// <summary>
        /// Publishes a <see cref="BbEvent"/> through the pipeline.
        /// </summary>
        /// <param name="bbEvent">The event that we want to publish.</param>
        public void Publish(BbEvent bbEvent)
        {
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
        /// Used internal by BigBrother to publish usage exceptions to a special
        /// Application Insights account.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that we want to publish.</param>
        internal static void PublishError(Exception ex)
        {
            InternalStream.OnNext(ex.ToBbEvent());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// true if called by Dispose(), where we also want to Dispose managed resources,
        /// false if called by the finalizer where we do not want to dispose of managed resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var key in _telemetrySubscriptions.Keys)
                {
                    _telemetrySubscriptions[key]?.Dispose();
                }

                _internalSubscription?.Dispose();
            }
        }
    }
}
