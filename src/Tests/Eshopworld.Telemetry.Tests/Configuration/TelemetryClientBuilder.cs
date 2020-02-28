using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Eshopworld.Telemetry.Tests.Configuration
{
    class TelemetryClientBuilder
    {
        private readonly List<ITelemetryInitializer> _initializers = new List<ITelemetryInitializer>();

        private readonly List<ITelemetryModule> _telemetryModules = new List<ITelemetryModule>();

        public TelemetryClientBuilder AddInitializer(ITelemetryInitializer telemetryInitializer)
        {
            _initializers.Add(telemetryInitializer);
            return this;
        }

        public TelemetryClientBuilder AddInitializers(IEnumerable<ITelemetryInitializer> telemetryInitializers)
        {
            _initializers.AddRange(telemetryInitializers);
            return this;
        }

        public TelemetryClientBuilder AddModules(ITelemetryModule telemetryModule)
        {
            _telemetryModules.Add(telemetryModule);
            return this;
        }

        public TelemetryClient Build(Action<ITelemetry> sendTelemetryAction)
        {
            var channel = new TestTelemetryChannel(sendTelemetryAction);
            var telemetryConfiguration = new TelemetryConfiguration(Guid.NewGuid().ToString(), channel);
            foreach (var initializer in _initializers)
                telemetryConfiguration.TelemetryInitializers.Add(initializer);
            foreach (var module in _telemetryModules)
                module.Initialize(telemetryConfiguration);
            return new TelemetryClient(telemetryConfiguration);
        }

        private sealed class TestTelemetryChannel : ITelemetryChannel
        {
            private readonly Action<ITelemetry> _sendAction;

            public bool? DeveloperMode { get; set; }
            public string EndpointAddress { get; set; }

            public TestTelemetryChannel(Action<ITelemetry> sendAction)
            {
                _sendAction = sendAction;
            }

            public void Dispose()
            {
                // not needed
            }

            public void Flush()
            {
                // not needed
            }

            public void Send(ITelemetry item)
                => _sendAction(item);
        }
    }
}
