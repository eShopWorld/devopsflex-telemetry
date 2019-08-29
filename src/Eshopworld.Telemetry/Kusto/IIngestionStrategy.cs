using System;
using System.Threading.Tasks;
using Eshopworld.Core;
using Kusto.Data.Common;

namespace Eshopworld.Telemetry.Kusto
{
    /// <summary>
    /// Kusto ingestion strategy
    /// </summary>
    public interface IIngestionStrategy : IDisposable
    {
        /// <summary>
        /// Sends or schedules an event for kusto ingestion
        /// </summary>
        Task HandleKustoEvent<T>(T @event) where T:TelemetryEvent;

        /// <summary>
        /// Initialize connection clients
        /// </summary>
        void Setup(KustoDbDetails dbDetails, ICslAdminProvider adminProvider);
    }
}