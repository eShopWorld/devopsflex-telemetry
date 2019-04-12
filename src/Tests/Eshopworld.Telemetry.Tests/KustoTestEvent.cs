using System;
using Eshopworld.Core;
using Eshopworld.Tests.Core;

// ReSharper disable once CheckNamespace
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
