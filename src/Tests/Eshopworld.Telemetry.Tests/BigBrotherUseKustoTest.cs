using System;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using Xunit;

// ReSharper disable once CheckNamespace
public class BigBrotherUseKustoTest
{
    private string KustoUri;
    private string KustoDatabase;
    private string KustoTenantId;
    private string KustoAppId;
    private string KustoAppKey;

    public BigBrotherUseKustoTest()
    {
        KustoUri = Environment.GetEnvironmentVariable("kusto_uri", EnvironmentVariableTarget.Machine);
        KustoDatabase = Environment.GetEnvironmentVariable("kusto_database", EnvironmentVariableTarget.Machine);
        KustoTenantId = Environment.GetEnvironmentVariable("kusto_tenant_id", EnvironmentVariableTarget.Machine);
        KustoAppId = Environment.GetEnvironmentVariable("kusto_app_id", EnvironmentVariableTarget.Machine);
        KustoAppKey = Environment.GetEnvironmentVariable("kusto_app_key", EnvironmentVariableTarget.Machine);
    }

    [Fact, IsLayer1]
    public void Test_KustoTestEvent_StreamsToKusto()
    {
        var bb = new BigBrother("", "");
        bb.UseKusto(KustoUri, KustoDatabase, KustoTenantId, KustoAppId, KustoAppKey);

        bb.Publish(new KustoTestEvent());

        // Assert somehow ...
    }
}

public class KustoTestEvent : DomainEvent
{
    public KustoTestEvent()
    {
        SomeInt = new Random().Next(100);
        SomeStringOne = Lorem.GetSentence();
        SomeStringTwo = Lorem.GetSentence();
        SomeDateTime = DateTime.Now;
        SomeTimeSpan = TimeSpan.FromMinutes(new Random().Next(60));
    }

    public int SomeInt { get; set; }

    public string SomeStringOne { get; set; }

    public string SomeStringTwo { get; set; }

    public DateTime SomeDateTime { get; set; }

    public TimeSpan SomeTimeSpan { get; set; }
}
