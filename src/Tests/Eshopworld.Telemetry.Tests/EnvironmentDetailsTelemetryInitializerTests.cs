using System;
using System.Linq;
using Eshopworld.Tests.Core;
using Eshopworld.Telemetry.Initializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class EnvironmentDetailsTelemetryInitializerTests
{
    private readonly Mock<ITelemetry> mockTelemetry;
    private readonly TelemetryContext context;

    public EnvironmentDetailsTelemetryInitializerTests()
    {
        mockTelemetry = new Mock<ITelemetry>();
        context = new TelemetryContext();
        mockTelemetry.SetupGet(p => p.Context).Returns(context);
    }

    [Fact, IsUnit]
    public void UsingDefaultCtor_InitializeWithNoEnvVarSet_PropertiesExistWithNullValues()
    {
        // Arrange
        Array.ForEach(EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables, n => Environment.SetEnvironmentVariable(n, null)); // ensure no hangover from previous tests
        var ti = new EnvironmentDetailsTelemetryInitializer();

        // Act
        ti.Initialize(mockTelemetry.Object);

        // Assert
        Assert.Equal(context.GlobalProperties.Keys.OrderBy(n => n), EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables.Select(s => $"esw-{s}").OrderBy(n => n));
        Assert.All(context.GlobalProperties, p => Assert.Null(p.Value));
    }

    [Fact, IsUnit]
    public void UsingDefaultCtor_InitializeWithEnvVarSet_PropertiesExistWithValues()
    {
        // Arrange
        Array.ForEach(EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables, n => Environment.SetEnvironmentVariable(n, "foo"));
        var ti = new EnvironmentDetailsTelemetryInitializer(); // ensure it pulls latest env var values

        // Act
        ti.Initialize(mockTelemetry.Object);

        // Assert
        Assert.Equal(context.GlobalProperties.Keys.OrderBy(n => n), EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables.Select(s => $"esw-{s}").OrderBy(n => n));
        Assert.All(context.GlobalProperties, p => Assert.Equal("foo", p.Value));
    }

    [Fact, IsUnit]
    public void UsingConfigurationCtor_InitializeWithNoEnvVarSet_PropertiesExistWithNullValues()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var ti = new EnvironmentDetailsTelemetryInitializer(builder.Build());

        // Act
        ti.Initialize(mockTelemetry.Object);

        // Assert
        Assert.Equal(context.GlobalProperties.Keys.OrderBy(n => n), EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables.Select(s => $"esw-{s}").OrderBy(n => n));
        Assert.All(context.GlobalProperties, p => Assert.Null(p.Value));
    }

    [Fact, IsUnit]
    public void UsingConfigurationCtor_InitializeWithEnvVarSet_PropertiesExistWithValues()
    {
        // Arrange
        Array.ForEach(EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables, n => Environment.SetEnvironmentVariable(n, "foo"));
        var builder = new ConfigurationBuilder().AddEnvironmentVariables();
        var ti = new EnvironmentDetailsTelemetryInitializer(builder.Build()); // ensure it pulls latest env var values

        // Act
        ti.Initialize(mockTelemetry.Object);

        // Assert
        Assert.Equal(context.GlobalProperties.Keys.OrderBy(n => n), EnvironmentDetailsTelemetryInitializer.RequiredEnvironmentVariables.Select(s => $"esw-{s}").OrderBy(n => n));
        Assert.All(context.GlobalProperties, p => Assert.Equal("foo", p.Value));
    }
}