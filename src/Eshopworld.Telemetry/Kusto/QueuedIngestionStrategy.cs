using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Eshopworld.Core;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;

namespace Eshopworld.Telemetry.Kusto
{
    /// <summary>
    /// Queued ingestion client
    /// </summary>
    public class QueuedIngestionStrategy : IIngestionStrategy
    {
        internal readonly Dictionary<Type, KustoQueuedIngestionProperties> KustoMappings = new Dictionary<Type, KustoQueuedIngestionProperties>();

        private KustoDbDetails _dbDetails;
        private ICslAdminProvider _adminProvider;
        private IKustoQueuedIngestClient _ingestClient;

        private readonly CancellationToken _cancellationToken;
        private readonly int _timerInterval;
        private readonly int _maxItemCount;
        private readonly bool _flushImmediately;

        private bool _disposed = false;

        private Metric _ingestionTimeMetric;
        private ISubject<TelemetryEvent> _errorStream;
        private readonly ConcurrentDictionary<Type, Subject<TelemetryEvent>> _typeStream = new ConcurrentDictionary<Type, Subject<TelemetryEvent>>();

        private Subject<TelemetryEvent> _stream = new Subject<TelemetryEvent>();

        /// <summary>
        /// Ingestion strategy that uses local message buffer and Kusto Data Management Cluster for buffering and aggregation
        /// </summary>
        /// <param name="timerInterval">Local buffer flush interval</param>
        /// <param name="maxItemCount">Max messages in local buffer, flush immediately when reached</param>
        /// <param name="flushImmediately">Aggregate/buffer in Kusto Data Management Cluster or flush immediately to Kusto Engine</param>
        public QueuedIngestionStrategy(CancellationToken cancellationToken, int timerInterval = 1000, int maxItemCount = 100, bool flushImmediately = true)
        {
            _cancellationToken = cancellationToken;
            _timerInterval = timerInterval;
            _maxItemCount = maxItemCount;
            _flushImmediately = flushImmediately;
        }

        /// <inheritdoc />
        public void Setup(KustoDbDetails dbDetails, ICslAdminProvider adminProvider, ISubject<TelemetryEvent> errorStream, Metric ingestionTimeMetrics)
        {
            _dbDetails = dbDetails;
            _adminProvider = adminProvider;
            _errorStream = errorStream ?? throw new ArgumentNullException(nameof(errorStream));
            _ingestionTimeMetric = ingestionTimeMetrics ?? throw new ArgumentNullException(nameof(ingestionTimeMetrics));

            var kustoIngestUri = $"https://ingest-{dbDetails.Engine}.{dbDetails.Region}.kusto.windows.net";

            _ingestClient = KustoIngestFactory.CreateQueuedIngestClient(new KustoConnectionStringBuilder(kustoIngestUri)
            {
                FederatedSecurity = true,
                InitialCatalog = dbDetails.DbName,
                Authority = dbDetails.ClientId,
                ApplicationToken = dbDetails.AuthToken
            });

            var disposable = _stream.Buffer(TimeSpan.FromMilliseconds(_timerInterval), _maxItemCount)
                .SubscribeOn(Scheduler.Default)
                .SelectMany(FlushBuffer)
                .Subscribe();
        }

        /// <inheritdoc />
        public async Task HandleKustoEvent<T>(T @event) where T:TelemetryEvent
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(QueuedIngestionStrategy), "Queued Ingestion client is disposed");

            var eventType = @event.GetType();

            await VerifyTableAndModelProperties<T>(eventType);

            //var subject = _typeStream.GetOrAdd(eventType, _ =>
            //{
            //    var eventSubject = new Subject<TelemetryEvent>();
            //    eventSubject
            //        .Buffer(TimeSpan.FromMilliseconds(_timerInterval), _maxItemCount)
            //        .SubscribeOn(Scheduler.Default)
            //        .SelectMany(FlushBuffer)
            //        .Subscribe();

            //    return eventSubject;
            //});

            _stream.OnNext(@event);

            //subject.OnNext(@event);
        }

        private async Task<Unit> FlushBuffer(IList<TelemetryEvent> events)
        {
            if (events == null || events.Count == 0)
                return Unit.Default;

            try
            {
                var beginTime = DateTime.Now;

                var eventTypeGroups = events.GroupBy(x => x.GetType());

                foreach (var typeGroup in eventTypeGroups)
                {
                    if (typeGroup.Any())
                        await DispatchEvents(typeGroup, typeGroup.Key);
                }

                //await DispatchEvents(events, events[0].GetType());

                _ingestionTimeMetric.TrackValue(DateTime.Now.Subtract(beginTime).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _errorStream.OnNext(ex.ToExceptionEvent());
            }

            return Unit.Default;
        }

        private async Task DispatchEvents(IEnumerable<TelemetryEvent> events, Type eventType)
        {
            var ingestProps = KustoMappings[eventType];

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                foreach (var telemetryEvent in events)
                    writer.WriteLine(JsonConvert.SerializeObject(telemetryEvent));
                
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                await _ingestClient.IngestFromStreamAsync(stream, ingestProps, true);
            }
        }

        /// <summary>
        /// Create table mappings
        /// </summary>
        private async Task VerifyTableAndModelProperties<T>(Type eventType) where T : TelemetryEvent
        {
            if (!KustoMappings.ContainsKey(eventType))
            {
                var ingestProps = new KustoQueuedIngestionProperties(_dbDetails.DbName, eventType.Name)
                {
                    TableName = await _adminProvider.GenerateTableFromTypeAsync(eventType),
                    JSONMappingReference = await  _adminProvider.GenerateTableJsonMappingFromTypeAsync(eventType),
                    ReportLevel = IngestionReportLevel.FailuresOnly,
                    ReportMethod = IngestionReportMethod.Queue,
                    FlushImmediately = _flushImmediately,
                    IgnoreSizeLimit = true,
                    ValidationPolicy = null,
                    Format = DataSourceFormat.json
                };

                KustoMappings.Add(eventType, ingestProps);
            }
        }

        public void Dispose()
        {
            foreach (var subject in _typeStream)
            {
                subject.Value.Dispose();
            }

            _adminProvider?.Dispose();
            _ingestClient?.Dispose();
            _disposed = true;
        }
    }
}