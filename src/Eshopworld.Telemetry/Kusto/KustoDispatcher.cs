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
        private readonly IList<IIngestionStrategy> _ingestionStrategies; // hmmm
        private ICslAdminProvider _adminProvider;

        public KustoDispatcher(IList<IIngestionStrategy> ingestionStrategies, KustoDbDetails dbDetails)
        {
            _ingestionStrategies = ingestionStrategies;

            var kustoUri = $"https://{dbDetails.DbName}.{dbDetails.Region}.kusto.windows.net";
            dbDetails.AuthToken = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

            _adminProvider = KustoClientFactory.CreateCslAdminProvider(
                new KustoConnectionStringBuilder(kustoUri)
                {
                    FederatedSecurity = true,
                    InitialCatalog = dbDetails.DbName,
                    Authority = dbDetails.ClientId,
                    ApplicationToken = dbDetails.AuthToken
                });

            foreach (var strategy in ingestionStrategies)
            {
                strategy.Setup(dbDetails, _adminProvider);
            }
        }

        /// <inheritdoc />
        public virtual IDisposable Subscribe<T, S>(Subject<BaseEvent> stream, Metric ingestionTimeMetrics, ISubject<TelemetryEvent> errorStream) 
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
                .Select(e => Observable.FromAsync(async () => await HandleEvent(e, strategy, ingestionTimeMetrics, errorStream)))
                .Merge()
                .Subscribe();

            return subscription;
        }

        private async Task HandleEvent<T>(T e, IIngestionStrategy strategy, Metric ingestionTimeMetrics, ISubject<TelemetryEvent> errorStream)
            where T:TelemetryEvent
        {
            try
            {
                var time = DateTime.Now;

                await strategy.HandleKustoEvent(e);

                ingestionTimeMetrics.TrackValue(DateTime.Now.Subtract(time).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                errorStream.OnNext(ex.ToExceptionEvent());
            }
        }
    }
}