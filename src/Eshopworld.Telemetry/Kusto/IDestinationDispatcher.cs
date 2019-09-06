using System;
using System.Reactive.Subjects;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry.Kusto
{
    /// <summary>
    /// Dispatch events to Kusto
    /// </summary>
    public interface IDestinationDispatcher
    {
        /// <summary>
        /// Subscribe to event stream
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <typeparam name="S">Strategy used for this event type</typeparam>
        /// <param name="stream">Event stream</param>
        /// <param name="ingestionTimeMetrics">Timed metrics</param>
        /// <param name="errorStream">Error metrics</param>
        /// <returns>Disposable subscription</returns>
        IDisposable Subscribe<T, S>(Subject<BaseEvent> stream)
            where T : TelemetryEvent
            where S : IIngestionStrategy;

        void Initialise(KustoDbDetails dbDetails, Metric ingestionTimeMetrics, ISubject<TelemetryEvent> errorStream);

        /// <summary>
        /// Registers new ingestion strategy
        /// </summary>
        void AddStrategy(IIngestionStrategy strategy);
    }
}