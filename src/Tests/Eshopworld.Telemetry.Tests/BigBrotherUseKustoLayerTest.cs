using System;
using System.Threading.Tasks;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.Services.AppAuthentication;
using Polly;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherUseKustoLayerTest
{
    private readonly string _kustoName;
    private readonly string _kustoLocation;
    private readonly string _kustoDatabase;
    private readonly string _kustoTenantId;
    private readonly ICslQueryProvider _kustoQueryClient;

    public BigBrotherUseKustoLayerTest()
    {
        _kustoName = Environment.GetEnvironmentVariable("kusto_name", EnvironmentVariableTarget.Machine);
        _kustoLocation = Environment.GetEnvironmentVariable("kusto_location", EnvironmentVariableTarget.Machine);
        _kustoDatabase = Environment.GetEnvironmentVariable("kusto_database", EnvironmentVariableTarget.Machine);
        _kustoTenantId = Environment.GetEnvironmentVariable("kusto_tenant_id", EnvironmentVariableTarget.Machine);

        if (_kustoName != null && _kustoLocation != null && _kustoDatabase != null && _kustoTenantId != null)
        {
            var kustoUri = $"https://{_kustoName}.{_kustoLocation}.kusto.windows.net";
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync(kustoUri, string.Empty).Result;

            _kustoQueryClient = KustoClientFactory.CreateCslQueryProvider(
            new KustoConnectionStringBuilder(kustoUri)
            {
                FederatedSecurity = true,
                InitialCatalog = _kustoDatabase,
                ApplicationToken = token
            });
        }
    }

    [Fact, IsLayer1]
    public async Task Test_KustoTestEvent_StreamsToKusto()
    {
        _kustoQueryClient.Should().NotBeNull();

        var bb = new BigBrother("", "");
        bb.UseKusto(_kustoName, _kustoLocation, _kustoDatabase, _kustoTenantId);

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
                var reader = await _kustoQueryClient.ExecuteQueryAsync(
                    _kustoDatabase,
                    $"{nameof(KustoTestEvent)} | where {nameof(KustoTestEvent.Id)} == \"{evt.Id}\" | summarize count()",
                    ClientRequestProperties.FromJsonString("{}"));

                reader.Read().Should().BeTrue();
                reader.GetInt64(0).Should().Be(1);
            });
    }
}