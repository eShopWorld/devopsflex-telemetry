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

    public interface IKustoClusterBuilder
    {
        /// <summary>
        /// Configures the Kusto connection details to be used while streaming to Kusto.
        /// </summary>
        /// <param name="engine">The name of the Kusto engine that we are targeting.</param>
        /// <param name="region">The region of the Kusto engine that we are targeting.</param>
        /// <param name="database">The Kusto database in the engine, that we are targeting.</param>
        /// <param name="tenantId">The tenant ID for the subscription where the Kusto engine is.</param>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsBuilder WithCluster(string engine, string region, string database, string tenantId);

        /// <summary>
        /// Configures the buffering options for Queued ingestion.
        /// </summary>
        /// <param name="options">The options to use for queued ingestion.</param>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsBuilder WithBufferOptions(BufferedClientOptions options);
    }

    public interface IKustoOptionsBuilder
    {
        /// <summary>
        /// Registers an assembly for Kusto ingestion.
        ///     It will scan all the types, in the assembly, that inherit from <see cref="TelemetryEvent"/> and register them.
        /// </summary>
        /// <param name="assembly">The assembly that we want to scan types for Kusto ingestion.</param>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsTypeBuilder RegisterAssembly(Assembly assembly);

        /// <summary>
        /// Registers <typeparamref name="T"/> type for Kusto ingestion.
        /// </summary>
        /// <typeparam name="T">The type we're registering for Kusto ingestion</typeparam>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsTypeBuilder RegisterType<T>() where T : TelemetryEvent;

        /// <summary>
        /// Register configured message types and ingestion strategies. 
        /// </summary>
        /// <param name="onMessageSent">Callback invoked after messages have been sent to Kusto</param>
        void Build(Action<long> onMessageSent = null);
    }

    public interface IKustoOptionsTypeBuilder
    {
        /// <summary>
        /// Use the Queued ingestion client for the types we just registered.
        /// </summary>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsBuilder WithQueuedClient();

        /// <summary>
        /// Use the Direct ingestion client for the types we just registered.
        /// </summary>
        /// <returns>Fluent API chain. Call Build() at the end.</returns>
        IKustoOptionsBuilder WithDirectClient();
    }
}