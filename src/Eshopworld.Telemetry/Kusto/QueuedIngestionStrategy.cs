using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Eshopworld.Core;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
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

        private bool _inProgress = false;
        private DateTime _lastIngestion;
        private readonly CancellationToken _cancellationToken;
        private readonly int _timerInterval;
        private readonly int _maxItemCount;
        private readonly bool _flushImmediately;

        private readonly int _internalClock = 200;
        private bool _disposed = false;

        private readonly ConcurrentDictionary<Type, BufferBlock<TelemetryEvent>> _eventBuffer = new ConcurrentDictionary<Type, BufferBlock<TelemetryEvent>>();

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
        public void Setup(KustoDbDetails dbDetails, ICslAdminProvider adminProvider)
        {
            _dbDetails = dbDetails;
            _adminProvider = adminProvider;

            var kustoIngestUri = $"https://ingest-{dbDetails.Engine}.{dbDetails.Region}.kusto.windows.net";

            _ingestClient = KustoIngestFactory.CreateQueuedIngestClient(new KustoConnectionStringBuilder(kustoIngestUri)
            {
                FederatedSecurity = true,
                InitialCatalog = dbDetails.DbName,
                Authority = dbDetails.ClientId,
                ApplicationToken = dbDetails.AuthToken
            });

            _lastIngestion = DateTime.Now;

            // background timer which checks if buffer should be flushed
            Task.Run(Timer, _cancellationToken);
        }

        /// <inheritdoc />
        public async Task HandleKustoEvent<T>(T @event) where T:TelemetryEvent
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(QueuedIngestionStrategy), "Queued Ingestion client is disposed");

            var eventType = @event.GetType();

            var bufferBlock = _eventBuffer.GetOrAdd(eventType, _ => new BufferBlock<TelemetryEvent>());
            
            await VerifyTableAndModelProperties<T>(eventType);

            await bufferBlock.SendAsync(@event, _cancellationToken);
        }

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

        private async Task Timer()
        {
            while (true)
            {
                // check every 200ms if there's enough items in the buffer, or configured time interval has passed
                await Task.Delay(_internalClock, _cancellationToken);

                if (!_inProgress &&
                    (_eventBuffer.Any(x => x.Value.Count >= _maxItemCount) || DateTime.Now.Subtract(_lastIngestion).TotalMilliseconds >= _timerInterval) &&
                    _eventBuffer.Any(x => x.Value.Count > 0))
                {
                    await ProcessBuffers();
                }

                if (_cancellationToken.IsCancellationRequested || _disposed)
                    break;
            }
        }

        private async Task ProcessBuffers()
        {
            // Kusto ingestion can take some time, so lets not start new uploads until this one finishes
            _inProgress = true;

            Debug.WriteLine("Processing buffers");

            foreach (var bufferBlock in _eventBuffer)
            {
                if (bufferBlock.Value.Count > 0)
                {
                    bufferBlock.Value.TryReceiveAll(out var events);
                    await DispatchEvents(events, bufferBlock.Key);
                }
            }

            // send again after last ingest has finished
            _lastIngestion = DateTime.Now;
            _inProgress = false;
        }

        private async Task DispatchEvents(IList<TelemetryEvent> events, Type eventType)
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

        public void Dispose()
        {
            _eventBuffer.Clear();
            _adminProvider?.Dispose();
            _ingestClient?.Dispose();
            _disposed = true;
        }
    }
}