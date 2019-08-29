using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly object _gate = new object();

        private KustoDbDetails _dbDetails;
        private ICslAdminProvider _adminProvider;
        private IKustoQueuedIngestClient _ingestClient;
        private Timer _timer;
        private bool _inProgress = false;

        private readonly Dictionary<Type, BufferBlock<TelemetryEvent>> _eventBuffer = new Dictionary<Type, BufferBlock<TelemetryEvent>>();

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

            _timer = new Timer(OnTick, null, 2000, 2000);
        }

        /// <inheritdoc />
        public async Task HandleKustoEvent<T>(T @event) where T:TelemetryEvent
        {
            if (_eventBuffer.ContainsKey(typeof(T)))
                _eventBuffer.Add(typeof(T), new BufferBlock<TelemetryEvent>());

            await _eventBuffer[typeof(T)].SendAsync(@event);
        }

        private void OnTick(object state)
        {
            if (_inProgress) return;

            lock (_gate)
            {
                _inProgress = true;

                foreach (var bufferBlock in _eventBuffer)
                {
                    if (bufferBlock.Value.Count > 0)
                    {
                        bufferBlock.Value.TryReceiveAll(out var events);
                        DispatchEvents(events, bufferBlock.Key).GetAwaiter().GetResult();
                    }
                }
            }
        }

        private async Task DispatchEvents(IList<TelemetryEvent> events, Type eventType)
        {
            KustoQueuedIngestionProperties ingestProps;
            if (!KustoMappings.ContainsKey(eventType))
            {
                ingestProps = new KustoQueuedIngestionProperties(_dbDetails.DbName, "Unknown")
                {
                    TableName = _adminProvider.GenerateTableFromType(eventType),
                    JSONMappingReference = _adminProvider.GenerateTableJsonMappingFromType(eventType),
                    ReportLevel = IngestionReportLevel.FailuresOnly,
                    ReportMethod = IngestionReportMethod.Queue,
                    FlushImmediately = true,
                    IgnoreSizeLimit = true,
                    ValidationPolicy = null,
                    Format = DataSourceFormat.json
                };

                KustoMappings.Add(eventType, ingestProps);
            }
            else
            {
                ingestProps = KustoMappings[eventType];
            }

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
            _timer?.Dispose();
        }
    }
}