using System.Collections.Generic;
using System.Reactive.Subjects;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry.Kusto
{
    public class KustoOptionsBuilder
    {
        private readonly IDestinationDispatcher _dispatcher;
        protected readonly Subject<BaseEvent> TelemetryStream;
        protected readonly Metric IngestionTimeMetric;
        protected readonly ISubject<TelemetryEvent> ErrorStream;
        private KustoDbDetails _dbDetails;

        protected string A;

        public KustoOptionsBuilder(IDestinationDispatcher dispatcher, Subject<BaseEvent> telemetryStream, Metric ingestionTimeMetric, ISubject<TelemetryEvent> errorStream)
        {
            _dispatcher = dispatcher;
            TelemetryStream = telemetryStream;
            IngestionTimeMetric = ingestionTimeMetric;
            ErrorStream = errorStream;
        }
        
        public KustoOptionsBuilder UseCluster(string engine, string region, string database, string tenantId)
        { 
            _dbDetails = new KustoDbDetails { ClientId = tenantId, DbName = database, Engine = engine, Region = region };

            _dispatcher.Initialise(_dbDetails);

            return this;
        }

        public KustoStrategyBuilder<S> Subscribe<S>(S strategy) where S : IIngestionStrategy
        {
            var strategyBuilder = new KustoStrategyBuilder<S>(strategy, _dispatcher, this);

            return strategyBuilder;
        }

        public class KustoStrategyBuilder<S> where S: IIngestionStrategy
        {
            private readonly IDestinationDispatcher _dispatcher;
            private readonly KustoOptionsBuilder _builder;

            public KustoStrategyBuilder(IIngestionStrategy strategy, IDestinationDispatcher dispatcher, KustoOptionsBuilder builder)
            {
                _dispatcher = dispatcher;
                _builder = builder;

                _dispatcher.AddStrategy(strategy);
            }

            public KustoStrategyBuilder<S> With<T>() where T : TelemetryEvent
            {
                _dispatcher.Subscribe<T, S>(_builder.TelemetryStream, _builder.IngestionTimeMetric, _builder.ErrorStream);
                return this;
            }
        }
    }
}