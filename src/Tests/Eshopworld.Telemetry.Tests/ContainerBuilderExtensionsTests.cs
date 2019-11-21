using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Telemetry.Configuration;
using Eshopworld.Telemetry.Tests.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

public class ContainerBuilderExtensionsTests
{
    [Fact, IsLayer0]
    public void ConfigureBigBrotherRegisterInitializer()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.Register(c => new TelemetryClientBuilder().Build(evt => { }));
        var instrumentationKey = Guid.NewGuid().ToString();
        containerBuilder.Register(c => new TelemetrySettings { InstrumentationKey = instrumentationKey, InternalKey = instrumentationKey });
        containerBuilder.RegisterModule<TelemetryModule>();

        containerBuilder.ConfigureBigBrother(bigBrother => bigBrother.KustoDbName = "aa");
        var container = containerBuilder.Build();

        var bb = container.Resolve<IBigBrother>() as BigBrother;
        bb.Should().NotBeNull();
        bb.KustoDbName.Should().Be("aa");
    }

    [Fact, IsLayer0]
    public void ConfigureBigBrotherRegisterInitializerUsingComponentContext()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.Register(c => new TelemetryClientBuilder().Build(evt => { }));
        var instrumentationKey = Guid.NewGuid().ToString();
        containerBuilder.Register(c => new TelemetrySettings { InstrumentationKey = instrumentationKey, InternalKey = instrumentationKey });
        containerBuilder.Register(c => new TestData { Value = "bb" });
        containerBuilder.RegisterModule<TelemetryModule>();

        containerBuilder.ConfigureBigBrother(
            (bigBrother, cc) => bigBrother.KustoDbName = cc.Resolve<TestData>().Value);
        var container = containerBuilder.Build();

        var bb = container.Resolve<IBigBrother>() as BigBrother;
        bb.Should().NotBeNull();
        bb.KustoDbName.Should().Be("bb");
    }

    [Fact, IsLayer0]
    public void AddStatefullServiceTelemetryRegistersNecessaryCompoents()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddStatefullServiceTelemetry();

        var container = containerBuilder.Build();
        container.IsRegistered<TelemetryClient>().Should().BeTrue();
        var initializers = container.Resolve<IEnumerable<ITelemetryInitializer>>();
        initializers.OfType<OperationCorrelationTelemetryInitializer>().Should().NotBeEmpty();
        initializers.OfType<HttpDependenciesParsingTelemetryInitializer>().Should().NotBeEmpty();
        var modules = container.Resolve<IEnumerable<ITelemetryModule>>();
        modules.OfType<DependencyTrackingTelemetryModule>().Should().NotBeEmpty();
        // It depends on an internal implementation of RegisterServiceFabricSupport
        containerBuilder.Properties.ContainsKey("__ServiceFabricRegistered").Should().BeTrue();
    }

    private class TestData
    {
        public string Value { get; set; }
    }
}
