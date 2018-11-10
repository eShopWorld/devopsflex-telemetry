using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Moq;
using Polly;
using System;
using System.Threading.Tasks;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherUseKustoTest
{
    private string KustoUri;
    private string KustoDatabase;
    private string KustoTenantId;
    private string KustoAppId;
    private string KustoAppKey;

    private ICslQueryProvider KustoQueryClient;

    public BigBrotherUseKustoTest()
    {
        KustoUri = Environment.GetEnvironmentVariable("kusto_uri", EnvironmentVariableTarget.Machine);
        KustoDatabase = Environment.GetEnvironmentVariable("kusto_database", EnvironmentVariableTarget.Machine);
        KustoTenantId = Environment.GetEnvironmentVariable("kusto_tenant_id", EnvironmentVariableTarget.Machine);
        KustoAppId = Environment.GetEnvironmentVariable("kusto_app_id", EnvironmentVariableTarget.Machine);
        KustoAppKey = Environment.GetEnvironmentVariable("kusto_app_key", EnvironmentVariableTarget.Machine);

        if (KustoUri != null && KustoDatabase != null && KustoTenantId != null && KustoAppId != null && KustoAppKey != null)
        {
            KustoQueryClient = KustoClientFactory.CreateCslQueryProvider(
            new KustoConnectionStringBuilder($"https://{KustoUri}.kusto.windows.net")
            {
                FederatedSecurity = true,
                InitialCatalog = KustoDatabase,
                AuthorityId = KustoTenantId,
                ApplicationClientId = KustoAppId,
                ApplicationKey = KustoAppKey
            });
        }

    }

    [Fact, IsLayer1]
    public async Task Test_KustoTestEvent_StreamsToKusto()
    {
        KustoQueryClient.Should().NotBeNull();

        var bb = new BigBrother("", "");
        bb.UseKusto(KustoUri, KustoDatabase, KustoTenantId, KustoAppId, KustoAppKey);

        var evt = new KustoTestEvent();
        bb.Publish(evt);

        await Policy.Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .ExecuteAsync(async () =>
            {
                var reader = await KustoQueryClient.ExecuteQueryAsync(KustoDatabase, $"{nameof(KustoTestEvent)} | where {nameof(KustoTestEvent.Id)} == \"{evt.Id}\" | summarize count()", ClientRequestProperties.FromJsonString("{}"));
                reader.Read().Should().BeTrue();
                reader.GetInt64(0).Should().Be(1);
            });
    }

    [Fact, IsUnit]
    public void Test_ExceptionTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.UseKusto(KustoUri, KustoDatabase, KustoTenantId, KustoAppId, KustoAppKey);
        bb.Object.Publish(new Exception().ToExceptionEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_TimedTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.UseKusto(KustoUri, KustoDatabase, KustoTenantId, KustoAppId, KustoAppKey);
        bb.Object.Publish(new KustoTestTimedEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_MetricTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.UseKusto(KustoUri, KustoDatabase, KustoTenantId, KustoAppId, KustoAppKey);
        bb.Object.Publish(new KustoTestMetricEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }
}

public class KustoTestEvent : DomainEvent
{
    public KustoTestEvent()
    {
        Id = Guid.NewGuid();
        SomeInt = new Random().Next(100);
        SomeStringOne = Lorem.GetSentence();
        SomeStringTwo = Lorem.GetSentence();
        SomeDateTime = DateTime.Now;
        SomeTimeSpan = TimeSpan.FromMinutes(new Random().Next(60));
    }

    public Guid Id { get; set; }

    public int SomeInt { get; set; }

    public string SomeStringOne { get; set; }

    public string SomeStringTwo { get; set; }

    public DateTime SomeDateTime { get; set; }

    public TimeSpan SomeTimeSpan { get; set; }
}

public class KustoTestTimedEvent : TimedTelemetryEvent { }

public class KustoTestMetricEvent : MetricTelemetryEvent { }
