using System;
using System.Collections.Generic;
using Eshopworld.Core;

namespace Eshopworld.Telemetry.Kusto
{
    public class KustoOptionsBuilder
    {
        private readonly Action<KustoOptionsBuilder> _onBuild;

        internal KustoDbDetails DbDetails;
        internal readonly IDictionary<IngestionClient, List<Type>> ClientTypes = new Dictionary<IngestionClient, List<Type>>();
        internal BufferedClientOptions BufferOptions;

        internal IngestionClient Fallback = IngestionClient.None;

        internal Action<long> OnMessageSent;

        internal KustoOptionsBuilder(Action<KustoOptionsBuilder> onBuild)
        {
            _onBuild = onBuild;
            BufferOptions = new BufferedClientOptions
            {
                BufferSizeItems = 100,
                FlushImmediately = true,
                IngestionInterval = TimeSpan.FromMilliseconds(1000)
            };
        }

        public KustoOptionsBuilder WithCluster(string engine, string region, string database, string tenantId)
        { 
            DbDetails = new KustoDbDetails { ClientId = tenantId, DbName = database, Engine = engine, Region = region };
            return this;
        }

        public KustoOptionsBuilder WithQueuedClient<T>(BufferedClientOptions options = null) where T : TelemetryEvent
        {
            var types = ClientTypes.GetOrAdd(IngestionClient.Queued, () => new List<Type>());

            types.Add(typeof(T));

            BufferOptions = options ?? BufferOptions;

            return this;
        }

        public KustoOptionsBuilder WithDirectClient<T>() where T : TelemetryEvent
        {
            var types = ClientTypes.GetOrAdd(IngestionClient.Direct, () => new List<Type>());

            types.Add(typeof(T));

            return this;
        }

        public KustoOptionsBuilder WithFallbackQueuedClient(BufferedClientOptions options = null)
        {
            if (Fallback == IngestionClient.Direct)
                throw new InvalidOperationException("Default already set to Direct client");

            Fallback = IngestionClient.Queued;

            BufferOptions = options ?? BufferOptions;

            return this;
        }

        public KustoOptionsBuilder WithFallbackDirectClient()
        {
            if (Fallback == IngestionClient.Queued)
                throw new InvalidOperationException("Default already set to Queued client");

            Fallback = IngestionClient.Direct;

            return this;
        }

        public void Build(Action<long> onMessageSent = null)
        {
            OnMessageSent = onMessageSent;
            _onBuild(this);
        }
    }

    internal enum IngestionClient
    {
        None,
        Queued,
        Direct
    }
}