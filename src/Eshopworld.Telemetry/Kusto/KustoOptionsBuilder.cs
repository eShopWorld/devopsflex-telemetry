namespace Eshopworld.Telemetry.Kusto
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Eshopworld.Core;

    public class KustoOptionsBuilder : IKustoClusterBuilder, IKustoOptionsBuilder, IKustoOptionsTypeBuilder
    {
        private readonly Action<KustoOptionsBuilder> _onBuild;

        private Action<long> _onMessageSent;
        private bool _optionsSet = false;

        internal KustoDbDetails DbDetails { get; private set; }

        internal BufferedClientOptions BufferOptions { get; private set; }

        internal List<Type> RegisteredTypes { get; } = new List<Type>();

        internal List<Type> RegisteredDirectTypes { get; } = new List<Type>();

        internal List<Type> RegisteredQueuedTypes { get; } = new List<Type>();

        internal KustoOptionsBuilder(Action<KustoOptionsBuilder> onBuild = null)
        {
            _onBuild = onBuild;
            BufferOptions = new BufferedClientOptions();
        }

        /// <inheritdoc />
        public IKustoOptionsBuilder WithCluster(string engine, string region, string database, string tenantId)
        {
            DbDetails = new KustoDbDetails { ClientId = tenantId, DbName = database, Engine = engine, Region = region };
            return this;
        }

        /// <inheritdoc />
        public IKustoOptionsBuilder WithBufferOptions(BufferedClientOptions options)
        {
            if (_optionsSet)
            {
                throw new InvalidOperationException("You can only set options once, and this Kusto Options Builder already has options set.");
            }

            _optionsSet = true;
            BufferOptions = options;

            return this;
        }

        /// <inheritdoc />
        public IKustoOptionsBuilder WithQueuedClient()
        {
            RegisteredQueuedTypes.AddRange(RegisteredTypes);
            RegisteredTypes.Clear();

            return this;
        }

        /// <inheritdoc />
        public IKustoOptionsBuilder WithDirectClient()
        {
            RegisteredDirectTypes.AddRange(RegisteredTypes);
            RegisteredTypes.Clear();

            return this;
        }

        /// <inheritdoc />
        public IKustoOptionsTypeBuilder RegisterAssembly(Assembly assembly)
        {
            RegisteredTypes.AddRange(assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(TelemetryEvent))));
            return this;
        }

        /// <inheritdoc />
        public IKustoOptionsTypeBuilder RegisterType<T>()
            where T : TelemetryEvent
        {
            RegisteredTypes.Add(typeof(T));
            return this;
        }

        /// <inheritdoc />
        public void Build(Action<long> onMessageSent = null)
        {
            _onMessageSent = onMessageSent;
            _onBuild(this);
        }

        internal void OnMessagesSent(long count)
        {
            _onMessageSent.Invoke(count);
        }
    }
}