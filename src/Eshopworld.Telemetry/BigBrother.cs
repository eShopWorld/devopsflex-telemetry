namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Core;
    using InternalEvents;
    using JetBrains.Annotations;
    using Kusto.Data;
    using Kusto.Data.Common;
    using Kusto.Data.Net.Client;
    using Kusto.Ingest;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Metrics;
    using Microsoft.Azure.Services.AppAuthentication;
    using Newtonsoft.Json;

    /// <summary>
    /// Deals with everything that's public in telemetry.
    /// This is the main entry point in the <see cref="N:Eshopworld.Telemetry"/> API.
    /// </summary>
    public class BigBrother : IBigBrother, IDisposable
    {
        private object _gate = new object();

        /// <summary>
        /// The internal telemetry stream, used by packages to report errors and usage to an internal AI account.
        /// </summary>
        internal static readonly ISubject<TelemetryEvent> InternalStream = new Subject<TelemetryEvent>();

        /// <summary>
        /// The internal Exception stream, used to push direct exceptions to non-telemetry sinks.
        /// </summary>
        internal static readonly ISubject<ExceptionEvent> ExceptionStream = new Subject<ExceptionEvent>();

        /// <summary>
        /// The one time replayable internal Exception stream, used to push direct exceptions to non-telemetry sinks.
        ///     We use this to replay direct exceptions published before <see cref="BigBrother"/> ctor, only once.
        /// </summary>
        internal static readonly SingleReplayCast<ExceptionEvent> ReplayCast = new SingleReplayCast<ExceptionEvent>(ExceptionStream);

        /// <summary>
        /// The main event stream that's exposed publicly (yea ... subjects are bad ... I'll redesign when and if time allows).
        /// </summary>
        internal readonly Subject<BaseEvent> TelemetryStream = new Subject<BaseEvent>();

        /// <summary>
        /// Contains an internal stream typed dictionary of all the subscriptions to different types of telemetry that instrument this package.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, IDisposable> InternalSubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        /// <summary>
        /// Contains an exception stream typed dictionary of all the subscriptions to different types of telemetry that instrument this package.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, IDisposable> ExceptionSubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        /// <summary>
        /// The unique internal <see cref="Microsoft.ApplicationInsights.TelemetryClient"/> used to stream events to the AI account.
        /// </summary>
        internal static readonly TelemetryClient InternalClient = new TelemetryClient();

        /// <summary>
        /// Contains a typed dictionary of all the subscriptions to different types of telemetry.
        /// </summary>
        internal readonly ConcurrentDictionary<Type, IDisposable> TelemetrySubscriptions = new ConcurrentDictionary<Type, IDisposable>();

        internal readonly Dictionary<Type, KustoQueuedIngestionProperties> KustoMappings = new Dictionary<Type, KustoQueuedIngestionProperties>();

        /// <summary>
        /// Contains the <see cref="IPublishEvents"/> instance used to publish to topics.
        /// </summary>
        internal IPublishEvents TopicPublisher;

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
        /// The external telemetry client, used to publish events through <see cref="BigBrother"/>.
        /// </summary>
        internal TelemetryClient TelemetryClient;

        /// <summary>
        /// The name of the Kusto database we are using.
        /// </summary>
        internal string KustoDbName;

        /// <summary>
        /// The <see cref="ICslAdminProvider"/> Kusto Admin client we use to setup tables and table mappings.
        /// </summary>
        internal ICslAdminProvider KustoAdminClient;

        /// <summary>
        /// The <see cref="IKustoQueuedIngestClient"/> used for Kusto data ingestion.
        /// </summary>
        internal IKustoIngestClient KustoIngestClient;

        internal Metric KustoIngestionTimeMetric;

        /// <summary>
        /// Static initialization of static resources in <see cref="BigBrother"/> instances.
        /// </summary>
        static BigBrother()
        {
            ExceptionSubscriptions.AddSubscription(typeof(EventSource), ExceptionStream.Subscribe(SinkToEventSource));
            ExceptionSubscriptions.AddSubscription(typeof(Trace), ExceptionStream.Subscribe(SinkToTrace));
        }

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
            SetupTelemetryClient(aiKey, internalKey);
            SetupSubscriptions();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BigBrother"/>.
        ///     Used to leverage an existing <see cref="TelemetryClient"/> to track correlation.
        ///     This constructor does a bit of work, so if you're mocking this, mock the <see cref="IBigBrother"/> contract instead.
        /// </summary>
        /// <param name="client">The application's existing <see cref="TelemetryClient"/>.</param>
        /// <param name="internalKey">The devops internal telemetry Application Insights instrumentation key.</param>
        public BigBrother([NotNull]TelemetryClient client, [NotNull]string internalKey)
            // ReSharper disable once AssignNullToNotNullAttribute
            : this((string)null, internalKey)
        {
            TelemetryClient = client;
        }

        /// <summary>
        /// Provides access to internal Rx resources for improved extensibility and testability.
        /// </summary>
        /// <param name="telemetryObservable">The main event <see cref="IObservable{BaseEvent}"/> that's exposed publicly.</param>
        /// <param name="telemetryObserver">The main event <see cref="IObserver{BaseEvent}"/> that's used when Publishing.</param>
        /// <param name="internalObservable">The internal <see cref="IObservable{BaseEvent}"/>, used by packages to report errors and usage to an internal AI account.</param>
        public void Deconstruct(out IObservable<BaseEvent> telemetryObservable, out IObserver<BaseEvent> telemetryObserver, out IObservable<BaseEvent> internalObservable)
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
            ExceptionStream.OnNext(ex.ToExceptionEvent());
        }

        /// <summary>
        /// Writes a <see cref="ExceptionEvent"/> directly to all available sinks outside Application Insights.
        ///     This method will not publish the exception to Application Insights, it will just sink it.
        /// </summary>
        /// <param name="exEvent">The <see cref="ExceptionEvent"/> we want to write.</param>
        public static void Write(ExceptionEvent exEvent)
        {
            ExceptionStream.OnNext(exEvent);
        }

        /// <inheritdoc />
        public void Publish<T>(
            T @event,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1) where T : TelemetryEvent
        {
            if (TopicPublisher != null && @event is DomainEvent)
            {
                TopicPublisher.Publish(@event).Wait();
            }

            if (@event is TelemetryEvent telemetryEvent)
            {
                telemetryEvent.CallerMemberName = callerMemberName;
                telemetryEvent.CallerFilePath = callerFilePath;
                telemetryEvent.CallerLineNumber = callerLineNumber;
            }

            if (@event is TimedTelemetryEvent timedEvent)
            {
                timedEvent.End();
            }

            TelemetryStream.OnNext(@event);
        }

        /// <inheritdoc />
        public void Publish(
            object @event,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
        {
            TelemetryStream.OnNext(
                new AnonymousTelemetryEvent(@event)
                {
                    CallerMemberName = callerMemberName,
                    CallerFilePath = callerFilePath,
                    CallerLineNumber = callerLineNumber
                });
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
        public void Flush()
        {
            InternalStream.OnNext(new FlushEvent()); // You're not guaranteed to flush this event
            TelemetryClient.Flush();
            InternalClient.Flush();
        }

        /// <inheritdoc />
        public IBigBrother UseKusto(string kustoEngineName, string kustoEngineLocation, string kustoDb, string tenantId)
        {
            return UseKusto(kustoEngineName, kustoEngineLocation, kustoDb, tenantId, null);
        }

        /// <remarks>
        /// This will be documented later, because for now I'm not changing the interface for BigBrother to avoid breaking changes overlapping with v3.
        /// </remarks>
        public IBigBrother UseKusto(string kustoEngineName, string kustoEngineLocation, string kustoDb, string tenantId, IKustoIngestClient client)
        {
            try
            {
                KustoDbName = kustoDb;
                var kustoUri = $"https://{kustoEngineName}.{kustoEngineLocation}.kusto.windows.net";
                var token = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

                KustoAdminClient = KustoClientFactory.CreateCslAdminProvider(
                    new KustoConnectionStringBuilder(kustoUri)
                    {
                        FederatedSecurity = true,
                        InitialCatalog = KustoDbName,
                        Authority = tenantId,
                        ApplicationToken = token
                    });

                if (client == null)
                {
                    var kustoIngestUri = $"https://ingest-{kustoEngineName}.{kustoEngineLocation}.kusto.windows.net";

                    KustoIngestClient = KustoIngestFactory.CreateQueuedIngestClient(
                        new KustoConnectionStringBuilder(kustoIngestUri)
                        {
                            FederatedSecurity = true,
                            InitialCatalog = KustoDbName,
                            Authority = tenantId,
                            ApplicationToken = token,
                        });
                }
                else
                {
                    KustoIngestClient = KustoIngestFactory.CreateDirectIngestClient(
                        new KustoConnectionStringBuilder(kustoUri)
                        {
                            FederatedSecurity = true,
                            InitialCatalog = KustoDbName,
                            Authority = tenantId,
                            ApplicationToken = token,
                        });
                }

                SetupKustoSubscription();

                KustoIngestionTimeMetric = InternalClient.GetMetric(new MetricIdentifier("", "KustoIngestionTime"));
            }
            catch (Exception e)
            {
                InternalStream.OnNext(e.ToExceptionEvent());
            }

            return this;
        }

        /// <summary>
        /// Sets up the internal telemetry clients, both the one used to push normal events and the one used to push internal instrumentation.
        /// </summary>
        /// <param name="aiKey">The application's Application Insights instrumentation key.</param>
        /// <param name="internalKey">The DevOps internal telemetry Application Insights instrumentation key.</param>
        internal void SetupTelemetryClient(string aiKey, [NotNull]string internalKey)
        {
            if (aiKey != null)
            {
                TelemetryClient = new TelemetryClient
                {
                    InstrumentationKey = aiKey
                };
            }

            InternalClient.InstrumentationKey = internalKey;
        }

        /// <summary>
        /// Sets up internal subscriptions, this isolates subscriptions from the actual constructor logic during tests.
        /// </summary>
        internal void SetupSubscriptions()
        {
            ReplayCast.Subscribe(HandleAiEvent); // volatile subscription, don't need to keep it

            TelemetrySubscriptions.AddSubscription(typeof(TelemetryEvent), TelemetryStream.OfType<TelemetryEvent>().Subscribe(HandleAiEvent));
            InternalSubscriptions.AddSubscription(typeof(TelemetryEvent), InternalStream.OfType<TelemetryEvent>().Subscribe(HandleInternalEvent));
            GlobalExceptionAiSubscription = ExceptionStream.Subscribe(HandleAiEvent);
        }

        internal void SetupKustoSubscription()
        {
            var subscription = TelemetryStream.OfType<TelemetryEvent>()
                                              .Where(e => !(e is ExceptionEvent) &&
                                                          !(e is MetricTelemetryEvent) &&
                                                          !(e is TimedTelemetryEvent))
                                              .Select(e => Observable.FromAsync(async () => await HandleKustoEvent(e)))
                                              .Merge()
                                              .Subscribe();

            TelemetrySubscriptions.AddSubscription(typeof(KustoExtensions), subscription);
        }

        /// <summary>
        /// Handles <see cref="ExceptionEvent"/> that is going to be sinked to an <see cref="EventSource"/>.
        /// </summary>
        /// <param name="event">The event we want to sink to the <see cref="EventSource"/>.</param>
        internal static void SinkToEventSource(ExceptionEvent @event)
        {
            ErrorEventSource.Log.Error(@event);
        }

        /// <summary>
        /// Handles <see cref="ExceptionEvent"/> that is going to be sinked to a <see cref="Trace"/>.
        /// </summary>
        /// <param name="event">The event we want to sink to the <see cref="Trace"/>.</param>
        internal static void SinkToTrace(ExceptionEvent @event)
        {
            Trace.TraceError($"{nameof(ExceptionEvent)}: {@event.Exception.Message} | StackTrace: {@event.Exception.StackTrace}");
        }

        /// <summary>
        /// Send an <see cref="EventTelemetry" /> for display in Diagnostic Search and aggregation in Metrics Explorer.
        /// Create a separate <see cref="EventTelemetry" /> instance for each call to TrackEvent(EventTelemetry).
        /// </summary>
        /// <param name="telemetry">An event log item.</param>
        /// <param name="isInternal">True if this is an internal event, false otherwise.</param>
        internal virtual void TrackEvent(EventTelemetry telemetry, bool isInternal = false)
        {
            if (isInternal)
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
        /// <param name="isInternal">True if this is an internal event, false otherwise.</param>
        internal virtual void TrackException(ExceptionTelemetry telemetry, bool isInternal = false)
        {
            if (isInternal)
            {
                InternalClient.TrackException(telemetry);
            }
            else
            {
                TelemetryClient.TrackException(telemetry);
            }
        }

        /// <summary>
        /// Handles external events that are fired by Publish.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual void HandleAiEvent(TelemetryEvent @event)
        {
            switch (@event)
            {
                case ExceptionEvent telemetry:
                    var tEvent = new ConvertEvent<ExceptionEvent, ExceptionTelemetry>(telemetry).ToTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    TrackException(tEvent);
                    break;

                case TimedTelemetryEvent telemetry:
                    TrackEvent(new ConvertEvent<TimedTelemetryEvent, EventTelemetry>(telemetry).ToTelemetry());
                    break;

                case AnonymousTelemetryEvent telemetry:
                    TrackEvent(new ConvertEvent<AnonymousTelemetryEvent, EventTelemetry>(telemetry).ToTelemetry());
                    break;

                default:
                    TrackEvent(new ConvertEvent<TelemetryEvent, EventTelemetry>(@event).ToTelemetry());
                    break;
            }
        }

        /// <summary>
        /// Handles external events that are fired by the <see cref="InternalStream"/>.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual void HandleInternalEvent(TelemetryEvent @event)
        {
            switch (@event)
            {
                case ExceptionEvent telemetry:
                    var tEvent = new ConvertEvent<ExceptionEvent, ExceptionTelemetry>(telemetry).ToTelemetry();
                    if (tEvent == null) return;

                    tEvent.SeverityLevel = SeverityLevel.Error;

                    TrackException(tEvent, true);
                    break;

                case TimedTelemetryEvent telemetry:
                    TrackEvent(new ConvertEvent<TimedTelemetryEvent, EventTelemetry>(telemetry).ToTelemetry(), true);
                    break;

                case AnonymousTelemetryEvent telemetry:
                    TrackEvent(new ConvertEvent<AnonymousTelemetryEvent, EventTelemetry>(telemetry).ToTelemetry(), true);
                    break;

                default:
                    TrackEvent(new ConvertEvent<TelemetryEvent, EventTelemetry>(@event).ToTelemetry(), true);
                    break;
            }
        }

        /// <summary>
        /// Handles a <see cref="TelemetryEvent"/> that is being streamed to Kusto.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        internal virtual async Task HandleKustoEvent(TelemetryEvent @event)
        {
            var eventType = @event.GetType();
            KustoQueuedIngestionProperties ingestProps;

            try
            {
                lock (_gate)
                {
                    if (!KustoMappings.ContainsKey(eventType))
                    {
                        ingestProps = new KustoQueuedIngestionProperties(KustoDbName, "Unknown");

                        ingestProps = KustoMappings[eventType];

                        ingestProps.TableName = KustoAdminClient.GenerateTableFromType(eventType);
                        ingestProps.JSONMappingReference = KustoAdminClient.GenerateTableJsonMappingFromType(eventType);
                        ingestProps.ReportLevel = IngestionReportLevel.FailuresOnly;
                        ingestProps.ReportMethod = IngestionReportMethod.Queue;
                        ingestProps.FlushImmediately = true;
                        ingestProps.IgnoreSizeLimit = true;
                        ingestProps.ValidationPolicy = null;
                        ingestProps.Format = DataSourceFormat.json;

                        KustoMappings.Add(eventType, ingestProps);
                    }
                    else
                    {
                        ingestProps = KustoMappings[eventType];
                    }
                }

                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(@event));
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);

                    var startTime = DateTime.UtcNow;

                    await KustoIngestClient.IngestFromStreamAsync(stream, ingestProps, true);

                    KustoIngestionTimeMetric.TrackValue(DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
                }
            }
            catch (Exception e)
            {
                InternalStream.OnNext(e.ToExceptionEvent());
            }
        }

        /// <summary>
        /// Used internal by BigBrother to publish usage exceptions to a special
        ///     Application Insights account.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> that we want to publish.</param>
        internal static void PublishError(Exception ex)
        {
            InternalStream.OnNext(ex.ToExceptionEvent());
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">true if disposing through GC, false if through the finalizer.</param>
        [ExcludeFromCodeCoverage]
        protected virtual void Dispose(bool disposing)
        {
            foreach (var key in TelemetrySubscriptions.Keys)
            {
                TelemetrySubscriptions[key]?.Dispose();
            }
            TelemetrySubscriptions.Clear();

            foreach (var key in InternalSubscriptions.Keys)
            {
                InternalSubscriptions[key]?.Dispose();
            }
            InternalSubscriptions.Clear();

            TelemetryClient.Flush();
            InternalClient.Flush();
        }
    }
}
