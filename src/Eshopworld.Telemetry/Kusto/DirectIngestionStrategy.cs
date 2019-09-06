using System.Reactive.Subjects;
using System.Threading.Tasks;
using Eshopworld.Core;
using Kusto.Data.Common;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry.Kusto
{
    public class DirectIngestionStrategy : IIngestionStrategy
    {
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task HandleKustoEvent<T>(T @event) where T : TelemetryEvent
        {
            throw new System.NotImplementedException();
        }

        public void Setup(KustoDbDetails dbDetails, ICslAdminProvider adminProvider, ISubject<TelemetryEvent> errorStream,
            Metric ingestionTimeMetrics)
        {
            throw new System.NotImplementedException();
        }
    }
}