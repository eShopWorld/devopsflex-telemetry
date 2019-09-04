using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry.Kusto
{
    public class KustoOptionsBuilder
    {
        internal readonly IDestinationDispatcher Dispatcher;
        internal readonly Subject<BaseEvent> TelemetryStream;
        internal readonly Metric IngestionTimeMetric;
        internal readonly ISubject<TelemetryEvent> ErrorStream;
        private KustoDbDetails _dbDetails;

        public KustoOptionsBuilder(IDestinationDispatcher dispatcher, Subject<BaseEvent> telemetryStream, Metric ingestionTimeMetric, ISubject<TelemetryEvent> errorStream)
        {
            Dispatcher = dispatcher;
            TelemetryStream = telemetryStream;
            IngestionTimeMetric = ingestionTimeMetric;
            ErrorStream = errorStream;
        }
        
        public KustoOptionsBuilder UseCluster(string engine, string region, string database, string tenantId)
        { 
            _dbDetails = new KustoDbDetails { ClientId = tenantId, DbName = database, Engine = engine, Region = region };

            Dispatcher.Initialise(_dbDetails);

            return this;
        }

        //internal KustoStrategyBuilder<S> Subscribe<S>(S strategy) where S : IIngestionStrategy
        //{
        //    var strategyBuilder = new KustoStrategyBuilder<S>(strategy, Dispatcher, this);

        //    return strategyBuilder;
        //}

        //public class KustoStrategyBuilder<S> where S: IIngestionStrategy
        //{
        //    private readonly IDestinationDispatcher _dispatcher;
        //    private readonly KustoOptionsBuilder _builder;

        //    public KustoStrategyBuilder(IIngestionStrategy strategy, IDestinationDispatcher dispatcher, KustoOptionsBuilder builder)
        //    {
        //        _dispatcher = dispatcher;
        //        _builder = builder;

        //        _dispatcher.AddStrategy(strategy);
        //    }

        //    public KustoStrategyBuilder<S> With<T>() where T : TelemetryEvent
        //    {
        //        _dispatcher.Subscribe<T, S>(_builder.TelemetryStream, _builder.IngestionTimeMetric, _builder.ErrorStream);
        //        return this;
        //    }
        //}
    }

    public static class QueuedClientBuilderExtensions
    {
        private static bool _strategyAdded = false;

        private static void Check(KustoOptionsBuilder builder, CancellationToken token, int bufferInterval, int maxBufferItems)
        {
            if (!_strategyAdded)
                builder.Dispatcher.AddStrategy(new QueuedIngestionStrategy(token, bufferInterval, maxBufferItems));

            _strategyAdded = true;
        }

        public static KustoOptionsBuilder UseQueuedIngestion<T>(this KustoOptionsBuilder builder, CancellationToken token, int bufferInterval = 1000, int maxBufferItems = 50)
            where  T:TelemetryEvent
        {
            Check(builder, token, bufferInterval, maxBufferItems);
            builder.Dispatcher.Subscribe<T, QueuedIngestionStrategy>(builder.TelemetryStream, builder.IngestionTimeMetric, builder.ErrorStream);
            return builder;
        }
    }
}