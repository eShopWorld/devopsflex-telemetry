using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Eshopworld.Core;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.Azure.Services.AppAuthentication;
using Newtonsoft.Json;

namespace Eshopworld.Telemetry
{
    public interface IDestinationDispatcher
    {

    }

    public class KustoDispatcher  : IDestinationDispatcher
    {
        private readonly IList<IIngestionStrategy> _ingestionStrategies; // hmmm

        private ICslAdminProvider _adminProvider;

        public KustoDispatcher(IList<IIngestionStrategy> ingestionStrategies, string dbName, string clientId, string engine, string region)
        {
            _ingestionStrategies = ingestionStrategies;

            var kustoUri = $"https://{engine}.{region}.kusto.windows.net";
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

            _adminProvider = KustoClientFactory.CreateCslAdminProvider(
                new KustoConnectionStringBuilder(kustoUri)
                {
                    FederatedSecurity = true,
                    InitialCatalog = dbName,
                    Authority = clientId,
                    ApplicationToken = token
                });

            foreach (var strategy in ingestionStrategies)
            {
                strategy.Setup(dbName, clientId, token, _adminProvider);
            }
        }

        public IDisposable Subscribe<T, S>(Subject<BaseEvent> stream) 
            where T : class
            where S : IIngestionStrategy
        {
            var strategy = _ingestionStrategies.OfType<S>().FirstOrDefault();

            var subscription = stream.OfType<T>()
                .Where(e => !(e is ExceptionEvent) &&
                            !(e is MetricTelemetryEvent) &&
                            !(e is TimedTelemetryEvent))
                .Select(e => Observable.FromAsync(async () => await strategy.HandleKustoEvent(e)))
                .Merge()
                .Subscribe();

            return subscription;
        }
    }

    public interface IIngestionStrategy
    {
        Task HandleKustoEvent<T>(T @event);
        void Setup(string dbName, string clientId, string token, ICslAdminProvider adminProvider);
    }

    public class QueuedIngestionStrategy : IIngestionStrategy
    {
        internal readonly Dictionary<Type, KustoQueuedIngestionProperties> KustoMappings = new Dictionary<Type, KustoQueuedIngestionProperties>();

        private readonly object _gate = new object();

        private string _dbName;
        private ICslAdminProvider _adminProvider;
        private IKustoQueuedIngestClient _ingestClient;

        public void Setup(string dbName, string clientId, string token, ICslAdminProvider adminProvider)
        {
            _dbName = dbName;
            _adminProvider = adminProvider;

            _ingestClient = KustoIngestFactory.CreateQueuedIngestClient(new KustoConnectionStringBuilder
            {
                FederatedSecurity = true,
                InitialCatalog = _dbName,
                Authority = clientId,
                ApplicationToken = token
            });
        }

        public async Task HandleKustoEvent<T>(T @event)
        {
            var eventType = @event.GetType();

            try
            {
                KustoQueuedIngestionProperties ingestProps;
                lock (_gate)
                {
                    if (!KustoMappings.ContainsKey(eventType))
                    {
                        ingestProps = new KustoQueuedIngestionProperties(_dbName, "Unknown")
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
                }

                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(@event));
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);

                    var startTime = DateTime.UtcNow;

                    await _ingestClient.IngestFromStreamAsync(stream, ingestProps, true);

                    //KustoIngestionTimeMetric.TrackValue(DateTime.UtcNow.Subtract(startTime).TotalMilliseconds);
                }
            }
            catch (Exception e)
            {
                //InternalStream.OnNext(e.ToExceptionEvent());
            }
        }
    }

    public class DirectIngestionStrategy : IIngestionStrategy
    {
        public Task HandleKustoEvent<T>(T @event)
        {
            return Task.CompletedTask;
        }

        public void Setup(string dbName, string clientId, string token, ICslAdminProvider adminProvider)
        {
            
        }
    }
}