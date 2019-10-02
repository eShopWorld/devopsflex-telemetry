namespace Eshopworld.Telemetry.Kusto
{
    using System;
    using System.Collections.Generic;
    using Eshopworld.Core;

    public class KustoOptionsBuilder
    {
        private readonly Action<KustoOptionsBuilder> _onBuild;

        internal KustoDbDetails DbDetails;

        private readonly Dictionary<IngestionClient, List<Type>> ClientTypes = new Dictionary<IngestionClient, List<Type>>();
        internal BufferedClientOptions BufferOptions;

        internal IngestionClient Fallback = IngestionClient.None;

        private Action<long> _onMessageSent;

        internal KustoOptionsBuilder(Action<KustoOptionsBuilder> onBuild)
        {
            _onBuild = onBuild;
            BufferOptions = new BufferedClientOptions();
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