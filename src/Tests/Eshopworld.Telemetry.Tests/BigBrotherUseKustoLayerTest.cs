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
    private readonly string _kustoUri;
    private readonly string _kustoIngestUri;
    private readonly string _kustoDatabase;
    private readonly ICslQueryProvider _kustoQueryClient;

    public BigBrotherUseKustoLayerTest()
    {
        _kustoUri = Environment.GetEnvironmentVariable("kusto_uri");
        _kustoIngestUri = Environment.GetEnvironmentVariable("kusto_ingest_uri");
        _kustoDatabase = Environment.GetEnvironmentVariable("kusto_database");

        if (_kustoUri != null && _kustoIngestUri != null && _kustoDatabase != null)
        {
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync(_kustoUri, string.Empty).Result;

            _kustoQueryClient = KustoClientFactory.CreateCslQueryProvider(
                new KustoConnectionStringBuilder(_kustoUri)
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
        bb.UseKusto(_kustoUri, _kustoIngestUri, _kustoDatabase);

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