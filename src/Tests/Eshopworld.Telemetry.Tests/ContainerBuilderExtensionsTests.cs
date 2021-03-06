﻿using System;
using Autofac;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Telemetry.Configuration;
using Eshopworld.Telemetry.Tests.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
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
    public void ConfigureTelemetryKeysRegistersKeys()
    {
        var containerBuilder = new ContainerBuilder();
        var instrumentationKey = Guid.NewGuid().ToString();
        var internalKey = Guid.NewGuid().ToString();

        containerBuilder.ConfigureTelemetryKeys(instrumentationKey, internalKey);

        var container = containerBuilder.Build();
        var telemetrySettings = container.Resolve<TelemetrySettings>();
        telemetrySettings.InstrumentationKey.Should().Be(instrumentationKey);
        telemetrySettings.InternalKey.Should().Be(internalKey);
    }

    private class TestData
    {
        public string Value { get; set; }
    }
}
