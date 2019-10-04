namespace Eshopworld.Telemetry.Kusto
{
    using System;
    using System.Collections.Generic;
    using Eshopworld.Core;

    public class KustoOptionsBuilder
    {
        private readonly Action<KustoOptionsBuilder> _onBuild;
        private readonly Dictionary<IngestionClient, List<Type>> ClientTypes = new Dictionary<IngestionClient, List<Type>>();
        private Action<long> _onMessageSent;

        internal KustoDbDetails DbDetails { get; private set;  }
        
        internal BufferedClientOptions BufferOptions { get; private set; }

        internal IngestionClient Fallback { get; private set; } = IngestionClient.None;

        internal KustoOptionsBuilder(Action<KustoOptionsBuilder> onBuild = null)
        {
            _onBuild = onBuild;
            BufferOptions = new BufferedClientOptions();
        }

        /// <summary>
        /// Configure Kusto cluster connection details
        /// </summary>
        public KustoOptionsBuilder WithCluster(string engine, string region, string database, string tenantId)
        { 
            DbDetails = new KustoDbDetails { ClientId = tenantId, DbName = database, Engine = engine, Region = region };
            return this;
        }

        /// <summary>
        /// Use queued buffered client for telemetry messages of type T
        /// </summary>
        /// <param name="options">Buffer configuration (max buffer size and ingestion interval)</param>
        /// <returns>Fluent builder. Call <see cref="Build"/> at the end!</returns>
        public KustoOptionsBuilder WithQueuedClient<T>(BufferedClientOptions options = null) where T : TelemetryEvent
        {
            var types = ClientTypes.GetOrAdd(IngestionClient.Queued, () => new List<Type>());

            types.Add(typeof(T));

            BufferOptions = options ?? BufferOptions;

            return this;
        }

        /// <summary>
        /// Use direct client for telemetry messages of type T
        /// </summary>
        /// <returns>Fluent builder. Call <see cref="Build"/> at the end!</returns>
        public KustoOptionsBuilder WithDirectClient<T>() where T : TelemetryEvent
        {
            var types = ClientTypes.GetOrAdd(IngestionClient.Direct, () => new List<Type>());

            types.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Use queued buffered client for all messages not explicitly registered   
        /// </summary>
        /// <param name="options"></param>
        /// <returns>Fluent builder. Call <see cref="Build"/> at the end!</returns>
        public KustoOptionsBuilder WithFallbackQueuedClient(BufferedClientOptions options = null)
        {
            if (Fallback == IngestionClient.Direct)
                throw new InvalidOperationException("Default already set to Direct client");

            Fallback = IngestionClient.Queued;

            BufferOptions = options ?? BufferOptions;

            return this;
        }

        /// <summary>
        /// Use direct client for all messages not explicitly registered   
        /// </summary>
        /// <returns>Fluent builder. Call <see cref="Build"/> at the end!</returns>
        public KustoOptionsBuilder WithFallbackDirectClient()
        {
            if (Fallback == IngestionClient.Queued)
                throw new InvalidOperationException("Default already set to Queued client");

            Fallback = IngestionClient.Direct;

            return this;
        }

        /// <summary>
        /// Register configured message types and ingestion strategies. 
        /// </summary>
        /// <param name="onMessageSent">Callback invoked after messages have been sent to Kusto</param>
        public void Build(Action<long> onMessageSent = null)
        {
            _onMessageSent = onMessageSent;
            _onBuild(this);
        }

        internal static KustoOptionsBuilder Default() => new KustoOptionsBuilder(null).WithFallbackQueuedClient();

        internal bool IsRegisteredOrDefault(IngestionClient client, Type type)
        {
            // event type is registered for current client (queued, direct) type
            if (ClientTypes.ContainsKey(client) && ClientTypes[client].Contains(type))
                return true;

            var other = client == IngestionClient.Direct ? IngestionClient.Queued : IngestionClient.Direct;
            var notInOtherClient = ClientTypes.ContainsKey(other) && !ClientTypes[other].Contains(type);
            var otherClientDoesNotExists = !ClientTypes.ContainsKey(other);

            // fallback method: event type is not registered for current client type,
            // so lets check if there's default client, and if this event type is not
            // registered for another client
            if (Fallback == client && (notInOtherClient || otherClientDoesNotExists))
                return true;

            return false;
        }

        internal void OnMessagesSent(long count)
        {
            _onMessageSent.Invoke(count);
        }
    }

    public enum IngestionClient
    {
        None,
        Queued,
        Direct
    }
}