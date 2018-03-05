namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
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
    /// This is the main entry point in the <see cref="N:DevOpsFlex.Telemetry" /> API.
    /// </summary>
    public class BigBrother : IBigBrother, IDisposable
    {
        /// <summary>
        /// This exists to make the class testable and to allow control over the "Now" during a test.
        /// </summary>
        internal static Func<DateTime> Now = () => DateTime.Now;

        /// <summary>
        /// The internal telemetry stream, used by packages to report errors and usage to an internal AI account.
        /// </summary>
        internal static readonly ISubject<BbEvent> InternalStream = new Subject<BbEvent>();

        /// <summary>
        /// The internal Exception stream, used to push direct exceptions to non-telemetry sinks.
        /// </summary>
        internal static readonly ISubject<BbExceptionEvent> ExceptionStream = new Subject<BbExceptionEvent>();

        /// <summary>
        /// The one time replayable internal Exception stream, used to push direct exceptions to non-telemetry sinks.
        ///     We use this to replay direct exceptions published before <see cref="BigBrother"/> ctor, only once.
        /// </summary>
        internal static readonly SingleReplayCast<BbExceptionEvent> ReplayCast = new SingleReplayCast<BbExceptionEvent>(ExceptionStream);

        /// <summary>
        /// The main event stream that's exposed publicly (yea ... subjects are bad ... I'll redesign when and if time allows).
        /// </summary>
        internal readonly Subject<BbEvent> TelemetryStream = new Subject<BbEvent>();

        /// <summary>
        /// Contains an internal stream typed dictionary of all the subscriptions to different types of telemetry that instrument this package.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, IDisposable> InternalSubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        /// <summary>
        /// Contains an exception stream typed dictionary of all the subscriptions to different types of telemetry that instrument this package.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, IDisposable> ExceptionSubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        /// <summary>
        /// Contains the exception stream typed dictionary of sink subscription.
        /// </summary>
        internal IDisposable EventSourceSinkSubscription;

        /// <summary>
        /// Contains the exception stream typed dictionary of sink subscription.
        /// </summary>
        internal IDisposable TraceSinkSubscription;

        /// <summary>
        /// Constans the static ExceptionStream subscription from this client.
        /// </summary>
        internal IDisposable GlobalExceptionAiSubscription;

        /// <summary>
        /// Contains a typed dictionary of all the subscriptions to different types of telemetry.
        /// </summary>
        internal ConcurrentDictionary<Type, IDisposable> TelemetrySubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        /// <summary>
        /// The unique internal <see cref="Microsoft.ApplicationInsights.TelemetryClient"/> used to stream events to the AI account.
        /// </summary>
        internal static readonly TelemetryClient InternalClient;

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

            ExceptionSubscriptions.AddSubscription(typeof(EventSource), ExceptionStream.Subscribe(SinkToEventSource));
            ExceptionSubscriptions.AddSubscription(typeof(Trace), ExceptionStream.Subscribe(SinkToTrace));
        }

        /// <summary>
        /// Contains a lookup reference for each lose correlation handle provided.
        /// </summary>
        internal readonly ConcurrentDictionary<object, CorrelationHandle> CorrelationHandles = new ConcurrentDictionary<object, CorrelationHandle>();

        /// <summary>
        /// Contains the timer used to clear old correlation handles from the lookup <see cref="Dictionary{TKey,TValue}"/>
        /// </summary>
        internal readonly Timer CorrelationReleaseTimer;

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
        /// Writes an <see cref="Exception"/> directly to all available sinks outside Application Insights.
        ///     This method will not publish the exception to Application Insights, it will just sink it.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> we want to write.</param>
        public static void Write(Exception ex)
        {
            ExceptionStream.OnNext(ex.ToBbEvent());
        }

        /// <summary>
        /// Writes a <see cref="BbExceptionEvent"/> directly to all available sinks outside Application Insights.
        ///     This method will not publish the exception to Application Insights, it will just sink it.
        /// </summary>
        /// <param name="exEvent">The <see cref="BbExceptionEvent"/> we want to write.</param>
        public static void Write(BbExceptionEvent exEvent)
        {
            ExceptionStream.OnNext(exEvent);
        }


        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IDisposable CreateCorrelation()
        {
            if (Handle == null) return new StrictCorrelationHandle(this);

            var ex = new InvalidOperationException("You're trying to create a second correlation handle while one is active. Use lose correlation instead if you're trying to correlate work in parallel with different correlations.");
#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
                throw ex;
            }
#endif
            PublishError(ex);
            return Handle;
        }

        /// <inheritdoc />
        public string GetCorrelationVector(object handle)
        {
            return CorrelationHandles.ContainsKey(handle)
                ? CorrelationHandles[handle].Vector
                : null;
        }

        /// <inheritdoc />
        public void Flush()
        {
            InternalStream.OnNext(new FlushEvent()); // You're not guaranteed to flush this event
            TelemetryClient.Flush();
            InternalClient.Flush();
        }

        /// <inheritdoc />
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
            ReplayCast.Subscribe(HandleAiEvent); // volatile subscription, don't need to keep it

            TelemetrySubscriptions.AddSubscription(typeof(BbTelemetryEvent), TelemetryStream.OfType<BbTelemetryEvent>().Subscribe(HandleAiEvent));
            InternalSubscriptions.AddSubscription(typeof(BbTelemetryEvent), InternalStream.OfType<BbTelemetryEvent>().Subscribe(HandleInternalEvent));
            GlobalExceptionAiSubscription = ExceptionStream.Subscribe(HandleAiEvent);
        }

        /// <summary>
        /// Handles <see cref="BbExceptionEvent"/> that is going to be sinked to an <see cref="EventSource"/>.
        /// </summary>
        /// <param name="event">The event we want to sink to the <see cref="EventSource"/>.</param>
        internal static void SinkToEventSource(BbExceptionEvent @event)
        {
            ErrorEventSource.Log.Error(@event);
        }

        /// <summary>
        /// Handles <see cref="BbExceptionEvent"/> that is going to be sinked to a <see cref="Trace"/>.
        /// </summary>
        /// <param name="event">The event we want to sink to the <see cref="Trace"/>.</param>
        internal static void SinkToTrace(BbExceptionEvent @event)
        {
            Trace.TraceError($"BbExceptionEvent: {@event.Exception.Message} | StackTrace: {@event.Exception.StackTrace}");
        }

        /// <summary>
        /// Send an <see cref="EventTelemetry" /> for display in Diagnostic Search and aggregation in Metrics Explorer.
        /// Create a separate <see cref="EventTelemetry" /> instance for each call to TrackEvent(EventTelemetry).
        /// </summary>
        /// <param name="telemetry">An event log item.</param>
        /// <param name="internal">True if this is an internal event, false otherwise.</param>
        internal virtual void TrackEvent(EventTelemetry telemetry, bool @internal = false)
        {
            if (@internal)
            {
                InternalClient.TrackEvent(telemetry);
            }
            else
            {
                TelemetryClient.TrackEvent(telemetry);
            }
        }

        /// <summary>
        /// Send an <see cref="ExceptionTelemetry" /> for display in Diagnostic Search.
        /// Create a separate <see cref="ExceptionTelemetry" /> instance for each call to TrackException(ExceptionTelemetry).
        /// </summary>
        /// <param name="telemetry">An event log item.</param>
        /// <param name="internal">True if this is an internal event, false otherwise.</param>
        internal virtual void TrackException(ExceptionTelemetry telemetry, bool @internal = false)
        {
            if (@internal)
            {
                InternalClient.TrackException(telemetry);
            }
            else
            {
                TelemetryClient.TrackException(telemetry);
            }
        }

        /// <summary>
        /// Handles external events that are fired by <see cref="Publish"/>.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual void HandleAiEvent(BbTelemetryEvent @event)
        {
            switch (@event)
            {
                case BbExceptionEvent telemetry:
                    var tEvent = telemetry.ToExceptionTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    TrackException(tEvent);
                    break;

                case BbTimedEvent telemetry:
                    TrackEvent(telemetry.ToTimedTelemetry());
                    break;

                default:
                    TrackEvent(@event.ToEventTelemetry());
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
                    var tEvent = ex.ToExceptionTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    TrackException(tEvent, true);
                    break;

                case BbTimedEvent te:
                    TrackEvent(te.ToTimedTelemetry(), true);
                    break;

                default:
                    TrackEvent(@event.ToEventTelemetry(), true);
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
            var now = Now(); // Do DateTime.Now once per tick to speed up the release->collect pass.

            foreach (var handle in CorrelationHandles.Where(h => !h.Value.IsAlive(now)).ToList())
            {
                CorrelationHandles.TryRemove(handle.Key, out var __);
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
                    CorrelationHandles.TryAdd(correlation, handle);
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
