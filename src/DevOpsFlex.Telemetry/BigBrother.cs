namespace DevOpsFlex.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using Core;
    using InternalEvents;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Deals with everything that's public in telemetry.
    /// This is the main entry point in the <see cref="Telemetry"/> API.
    /// </summary>
    public class BigBrother : IBigBrother, IDisposable
    {
        /// <summary>
        /// The unique internal <see cref="Microsoft.ApplicationInsights.TelemetryClient"/> used to stream events to the AI account.
        /// </summary>
        internal static readonly TelemetryClient InternalClient;

        /// <summary>
        /// The internal telemetry stream, used by packages to report errors and usage to an internal AI account.
        /// </summary>
        internal static readonly Subject<BbEvent> InternalStream = new Subject<BbEvent>();

        /// <summary>
        /// Contains an internal typed dictionary of all the subscriptions to different types of telemetry that instrument this package.
        /// </summary>
        internal static readonly Dictionary<Type, IDisposable> InternalSubscriptions = new Dictionary<Type, IDisposable>();

        /// <summary>
        /// The top level frame on the <see cref="StackTrace"/> when <see cref="BigBrother"/> was instanciated.
        ///     This is used mostly for correlation logic.
        /// </summary>
        internal static string BirthPlace;

        /// <summary>
        /// Static initialization of static resources in <see cref="BigBrother"/> instances.
        /// </summary>
        static BigBrother()
        {
            InternalClient = new TelemetryClient();
        }

        /// <summary>
        /// Contains a lookup reference for each lose correlation handle provided.
        /// </summary>
        internal readonly Dictionary<object, CorrelationHandle> CorrelationHandles = new Dictionary<object, CorrelationHandle>();

        /// <summary>
        /// Contains the timer used to clear old correlation handles from the lookup <see cref="Dictionary{TKey,TValue}"/>
        /// </summary>
        internal readonly Timer CorrelationReleaseTimer;

        /// <summary>
        /// The main event stream that's exposed publicly (yea ... subjects are bad ... I'll redesign when and if time allows).
        /// </summary>
        internal readonly Subject<BbEvent> TelemetryStream = new Subject<BbEvent>();

        /// <summary>
        /// Contains a typed dictionary of all the subscriptions to different types of telemetry.
        /// </summary>
        internal Dictionary<Type, IDisposable> TelemetrySubscriptions = new Dictionary<Type, IDisposable>();

        /// <summary>
        /// The external telemetry client, used to publish events through <see cref="BigBrother"/>.
        /// </summary>
        internal TelemetryClient TelemetryClient;

        /// <summary>
        /// Default keep alive <see cref="TimeSpan"/> for lose correlation handles created by the consumer.
        /// </summary>
        internal TimeSpan DefaultCorrelationKeepAlive = TimeSpan.FromMinutes(10);

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
        ///     This constructor does a bit of work, so if you're mocking this, mock the <see cref="IBigBrother"/> contract instead.
        /// </summary>
        /// <param name="aiKey">The application's Application Insights instrumentation key.</param>
        /// <param name="internalKey">The devops internal telemetry Application Insights instrumentation key.</param>
        public BigBrother([NotNull]string aiKey, [NotNull]string internalKey)
        {
            CorrelationReleaseTimer = new Timer(ReleaseCorrelationVectors, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            var trace = new StackTrace();
            BirthPlace = trace.GetFrame(trace.FrameCount - 1).GetMethod().DeclaringType?.FullName;

            SetupTelemetryClient(aiKey, internalKey);
            SetupSubscriptions();
        }

        /// <summary>
        /// Provides access to internal Rx resources for improved extensability and testability.
        /// </summary>
        /// <param name="telemetryObservable">The main event <see cref="IObservable{BbEvent}"/> that's exposed publicly.</param>
        /// <param name="telemetryObserver">The main event <see cref="IObserver{BbEvent}"/> that's used when Publishing.</param>
        /// <param name="internalObservable">The internal <see cref="IObservable{BbEvent}"/>, used by packages to report errors and usage to an internal AI account.</param>
        public void Deconstruct(out IObservable<BbEvent> telemetryObservable, out IObserver<BbEvent> telemetryObserver, out IObservable<BbEvent> internalObservable)
        {
            telemetryObservable = TelemetryStream.AsObservable();
            telemetryObserver = TelemetryStream.AsObserver();
            internalObservable = InternalStream.AsObservable();
        }

        /// <summary>
        /// Publishes a <see cref="BbEvent"/> through the pipeline.
        /// </summary>
        /// <param name="bbEvent">The event that we want to publish.</param>
        /// <param name="correlation">The correlation handle if you want to correlate events</param>
        public void Publish(BbEvent bbEvent, object correlation = null)
        {
            if (bbEvent is BbTimedEvent timedEvent)
            {
                timedEvent.End();
            }
            if (bbEvent is BbTelemetryEvent tEvent)
            {
                SetupCorrelation(tEvent, correlation);
            }

            TelemetryStream.OnNext(bbEvent);
        }

        /// <summary>
        /// Forces the telemetry channel to be in developer mode, where it will instantly push
        ///     telemetry to the Application Insights account.
        /// </summary>
        public IBigBrother DeveloperMode()
        {
#if DEBUG
            if (TelemetryConfiguration.Active != null && TelemetryConfiguration.Active.TelemetryChannel != null)
            {
                TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
            }
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
            TelemetryClient.Flush();
            InternalClient.Flush();
        }

        /// <summary>
        /// Sets the ammount of minutes to keep a lose correlation object reference alive.
        /// </summary>
        /// <param name="span">The <see cref="TimeSpan"/> to keep a lose correlation handle alive.</param>
        public void SetCorrelationKeepAlive(TimeSpan span)
        {
            DefaultCorrelationKeepAlive = span;
        }

        /// <summary>
        /// Sets up the internal telemetry clients, both the one used to push normal events and the one used to push internal instrumentation.
        /// </summary>
        /// <param name="aiKey">The application's Application Insights instrumentation key.</param>
        /// <param name="internalKey">The devops internal telemetry Application Insights instrumentation key.</param>
        internal void SetupTelemetryClient([NotNull]string aiKey, [NotNull]string internalKey)
        {
            TelemetryClient = new TelemetryClient
            {
                InstrumentationKey = aiKey
            };

            InternalClient.InstrumentationKey = internalKey;
        }

        /// <summary>
        /// Sets up internal subscriptions, this isolates subscriptioons from the actual constructor logic during tests.
        /// </summary>
        internal void SetupSubscriptions()
        {
            TelemetrySubscriptions.Add(
                typeof(BbTelemetryEvent),
                TelemetryStream.OfType<BbTelemetryEvent>().Subscribe(HandleEvent));

            InternalSubscriptions.Add(
                typeof(BbTelemetryEvent),
                InternalStream.OfType<BbTelemetryEvent>().Subscribe(HandleInternalEvent));
        }

        /// <summary>
        /// Handles external events that are fired by <see cref="Publish"/>.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual void HandleEvent(BbTelemetryEvent @event)
        {
            switch (@event)
            {
                case BbExceptionEvent ex:
                    var tEvent = ex.ToTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    TelemetryClient.TrackException(tEvent);
                    break;

                case BbTimedEvent te:
                    TelemetryClient.TrackEvent(te.ToTelemetry());
                    break;

                default:
                    TelemetryClient.TrackEvent(@event.ToTelemetry());
                    break;
            }
        }

        /// <summary>
        /// Handles external events that are fired by the <see cref="InternalStream"/>.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual void HandleInternalEvent(BbTelemetryEvent @event)
        {
            switch (@event)
            {
                case BbExceptionEvent ex:
                    var tEvent = ex.ToTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    InternalClient.TrackException(tEvent);
                    break;

                case BbTimedEvent te:
                    InternalClient.TrackEvent(te.ToTelemetry());
                    break;

                default:
                    InternalClient.TrackEvent(@event.ToTelemetry());
                    break;
            }
        }


        /// <summary>
        /// Used internal by BigBrother to publish usage exceptions to a special
        ///     Application Insights account.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that we want to publish.</param>
        internal static void PublishError(Exception ex)
        {
            InternalStream.OnNext(ex.ToBbEvent());
        }

        /// <summary>
        /// Does a periodic release of old correlation vector object references.
        /// </summary>
        /// <param name="_">[IGNORED] Timer state on callback.</param>
        internal void ReleaseCorrelationVectors(object _)
        {
            var now = DateTime.Now; // Do DateTime.Now once per tick to speed up the release->collect pass.

            foreach (var handle in CorrelationHandles.Where(h => h.Value.IsAlive(now)).ToList())
            {
                CorrelationHandles.Remove(handle.Key);
            }
        }

        /// <summary>
        /// Deals with all the details of setting up the correlation vector inside the event object.
        /// </summary>
        /// <param name="event">The event that we want to correlate through.</param>
        /// <param name="correlation">The correlation handle used to correlate.</param>
        internal void SetupCorrelation(BbTelemetryEvent @event, object correlation)
        {
            if (correlation != null) // lose > strict, so we override strict if a lose is passed in
            {
                if (CorrelationHandles.ContainsKey(correlation))
                {
                    @event.CorrelationVector = CorrelationHandles[correlation].Vector;
                    CorrelationHandles[correlation].Touch();
                }
                else
                {
                    var handle = new CorrelationHandle(DefaultCorrelationKeepAlive);
                    CorrelationHandles.Add(correlation, handle);
                    @event.CorrelationVector = handle.Vector;
                }
            }
            else if (Handle != null)
            {
                @event.CorrelationVector = Handle.Vector;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            CorrelationReleaseTimer?.Dispose();

            foreach (var key in TelemetrySubscriptions.Keys)
            {
                TelemetrySubscriptions[key]?.Dispose();
            }
            TelemetrySubscriptions.Clear();
            TelemetrySubscriptions = null;

            foreach (var key in InternalSubscriptions.Keys)
            {
                InternalSubscriptions[key]?.Dispose();
            }
            InternalSubscriptions.Clear();
        }
    }
}
