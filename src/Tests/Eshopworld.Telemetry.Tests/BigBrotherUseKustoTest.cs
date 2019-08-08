using System;
using System.Threading.Tasks;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.Services.AppAuthentication;
using Moq;
using Polly;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherUseKustoTest
{
    private readonly string KustoName;
    private readonly string KustoLocation;
    private string KustoDatabase;
    private string KustoTenantId;

    private ICslQueryProvider KustoQueryClient;
    private ICslAdminProvider KustoAdminClient;

    public BigBrotherUseKustoTest()
    {
        KustoName = Environment.GetEnvironmentVariable("kusto_name", EnvironmentVariableTarget.Machine);
        KustoLocation = Environment.GetEnvironmentVariable("kusto_location", EnvironmentVariableTarget.Machine);
        KustoDatabase = Environment.GetEnvironmentVariable("kusto_database", EnvironmentVariableTarget.Machine);
        KustoTenantId = Environment.GetEnvironmentVariable("kusto_tenant_id", EnvironmentVariableTarget.Machine);

        if (KustoName != null && KustoLocation != null && KustoDatabase != null && KustoTenantId != null)
        {
            var kustoUri = $"https://{KustoName}.{KustoLocation}.kusto.windows.net";
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

            var connectionStringBuilder = new KustoConnectionStringBuilder(kustoUri)
            {
                FederatedSecurity = true,
                InitialCatalog = KustoDatabase,
                AuthorityId = KustoTenantId,
                ApplicationToken = token
            };

            KustoQueryClient = KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
            KustoAdminClient = KustoClientFactory.CreateCslAdminProvider(connectionStringBuilder);
        }
    }

    [Fact, IsLayer1]
    public async Task Test_KustoTestEvent_StreamsToKusto()
    {
        KustoQueryClient.Should().NotBeNull();

        var bb = new BigBrother("", "");
        bb.UseKusto(KustoName, KustoLocation, KustoDatabase, KustoTenantId);

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
                var reader = await KustoQueryClient.ExecuteQueryAsync(
                    KustoDatabase,
                    $"{nameof(KustoTestEvent)} | where {nameof(KustoTestEvent.Id)} == \"{evt.Id}\" | summarize count()",
                    ClientRequestProperties.FromJsonString("{}"));

                reader.Read().Should().BeTrue();
                reader.GetInt64(0).Should().Be(1);
            });
    }

    [Fact, IsLayer1]
    public async Task Test_KustoTestEvent_Creates_Table()
    {
        KustoQueryClient.Should().NotBeNull();
        KustoAdminClient.Should().NotBeNull();

        var bb = new BigBrother("", "");
        bb.UseKusto(KustoName, KustoLocation, KustoDatabase, KustoTenantId);

        var dataReader = await KustoAdminClient.ExecuteControlCommandAsync(KustoDatabase, ".show tables");
        var tableExists = false;

        while (dataReader.Read())
        {
            var table = dataReader.GetString(0);
            if (table.Equals(nameof(KustoTestEvent), StringComparison.OrdinalIgnoreCase))
            {
                tableExists = true;
                break;
            }
        }

        if (tableExists)
            await KustoAdminClient.ExecuteControlCommandAsync(KustoDatabase, $".drop table {nameof(KustoTestEvent)}");
        
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
                var reader = await KustoQueryClient.ExecuteQueryAsync(
                    KustoDatabase,
                    $"{nameof(KustoTestEvent)} | where {nameof(KustoTestEvent.Id)} == \"{evt.Id}\" | summarize count()",
                    ClientRequestProperties.FromJsonString("{}"));

                reader.Read().Should().BeTrue();
                reader.GetInt64(0).Should().Be(1);
            });
    }

    [Fact, IsUnit]
    public void Test_ExceptionTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.SetupKustoSubscription();
        bb.Object.Publish(new Exception().ToExceptionEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_TimedTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.SetupKustoSubscription();
        bb.Object.Publish(new KustoTestTimedEvent());

        bb.Verify(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>()), Times.Never);
    }

    [Fact, IsUnit]
    public void Test_MetricTelemetry_DoesntStream_ToKusto()
    {
        var bb = new Mock<BigBrother>();
        bb.Setup(x => x.HandleKustoEvent(It.IsAny<TelemetryEvent>())).Verifiable();

        bb.Object.SetupKustoSubscription();
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
