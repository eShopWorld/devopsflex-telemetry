using System.Threading.Tasks;
using Eshopworld.Core;
using Kusto.Data.Common;

namespace Eshopworld.Telemetry.Kusto
{
    public class DirectIngestionStrategy : IIngestionStrategy
    {
        public Task HandleKustoEvent<T>(T @event) where T : TelemetryEvent
        {
            throw new System.NotImplementedException();
        }

        public void Setup(KustoDbDetails dbDetails, ICslAdminProvider adminProvider)
        {
            
        }

        public void Dispose()
        {
            
        }
    }
}