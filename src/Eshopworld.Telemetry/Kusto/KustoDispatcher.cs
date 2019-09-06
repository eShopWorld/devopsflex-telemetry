using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Eshopworld.Core;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Services.AppAuthentication;

namespace Eshopworld.Telemetry.Kusto
{
    /// <inheritdoc />
    public class KustoDispatcher  : IDestinationDispatcher
    {
        private KustoDbDetails _dbDetails;

        private readonly IList<IIngestionStrategy> _ingestionStrategies = new List<IIngestionStrategy>(); // hmmm
        private ISubject<TelemetryEvent> _errorStream;
        private Metric _ingestionTimeMetrics;

        private ICslAdminProvider _adminProvider;


        /// <summary>
        /// Setup Kusto admin provider
        /// </summary>
        /// <param name="dbDetails"></param>
        public void Initialise(KustoDbDetails dbDetails, Metric ingestionTimeMetrics, ISubject<TelemetryEvent> errorStream)
        {
            _dbDetails = dbDetails;
            _errorStream = errorStream;
            _ingestionTimeMetrics = ingestionTimeMetrics;

            var kustoUri = $"https://{dbDetails.Engine}.{dbDetails.Region}.kusto.windows.net";
            dbDetails.AuthToken = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

            _adminProvider = KustoClientFactory.CreateCslAdminProvider(
                new KustoConnectionStringBuilder(kustoUri)
                {
                    FederatedSecurity = true,
                    InitialCatalog = dbDetails.DbName,
                    Authority = dbDetails.ClientId,
                    ApplicationToken = dbDetails.AuthToken
                });
        }

        /// <inheritdoc />
        public void AddStrategy(IIngestionStrategy strategy)
        {
            _ingestionStrategies.Add(strategy);
            strategy.Setup(_dbDetails, _adminProvider, _errorStream, _ingestionTimeMetrics);
        }

        /// <inheritdoc />
        public virtual IDisposable Subscribe<T, S>(Subject<BaseEvent> stream) 
            where T : TelemetryEvent
            where S : IIngestionStrategy
        {
            var strategy = _ingestionStrategies.OfType<S>().FirstOrDefault();

            if (strategy == null)
                throw new Exception($"Strategy {typeof(S).Name} not registered"); // other exception type?

            var subscription = stream.OfType<T>()
                .Where(e => !(e is ExceptionEvent) &&
                            !(e is MetricTelemetryEvent) &&
                            !(e is TimedTelemetryEvent))
                .Select(e => Observable.FromAsync(async () => await HandleEvent(e, strategy)))
                .Merge()
                .Subscribe();

            return subscription;
        }


        /// <summary>
        /// Dispatch an event to strategy. Exceptions will be sent to the error stream.
        /// </summary>
        private async Task HandleEvent<T>(T e, IIngestionStrategy strategy)
            where T:TelemetryEvent
        {
            try
            {
                await strategy.HandleKustoEvent(e);
            }
            catch (Exception ex)
            {
                _errorStream.OnNext(ex.ToExceptionEvent());
            }
        }
    }
}