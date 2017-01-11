# devopsflex-telemetry

A telemetry abstraction, using an Application Insights based implementation through a Reactive Extensions pipeline.

### How does it work?

You instanciate `BigBrother` and then you use it to `Publish` events.
Ideally you store a singleton instance of `BigBrother` in your DI container and reference it by
it's interface `IBigBrother`.

### What are these events that I "publish"?

Events are strongly typed structures of data, written as
[POCOs](https://en.wikipedia.org/wiki/Plain_Old_CLR_Object).
They inherit from either `BbTelemetryEvent` if they are a pure event or from `BbExceptionEvent`
if they represent and exception in your application.

Here's a few examples:
```c#
public class MyExceptionEvent : BbExceptionEvent
{
    public string WhinerMessage { get; set; }

    public string Message { get; set; }
}

public class PaymentEvent : BbTelemetryEvent
{
    public DateTime ProcessedOn { get; set; }

    public float Ammount { get; set; }

    public string Currency { get; set; }
}
```

Now that you have some events, publishing them is just:
```c#
IBigBrother bb = new BigBrother("[Application Insights Key]", "[Application Insights Key - For inner telemetry]");

bb.Publish(new PaymentEvent
{
    ProcessedOn = DateTime.Now,
    Ammount = 100,
    Currency = "USD"
});
```

### How do I correlate events?

`BigBrother` supports two types of correlation: __Strict__ and __Lose__.

__Strict__ correlation is when you are doing syncronous processing and you only ever want
one correlation handle over time, so you get an IDisposable back to facilitate using blocks:
```C#
IBigBrother bb = new BigBrother("[Application Insights Key]", "[Application Insights Key - For inner telemetry]");

using (bb.CreateCorrelation())
{
    bb.Publish(new PaymentEvent
    {
        ProcessedOn = DateTime.Now,
        Ammount = 100,
        Currency = "USD"
    }); // These two events will have the same correlation ID

    bb.Publish(new PaymentEvent
    {
        ProcessedOn = DateTime.Now,
        Ammount = 200,
        Currency = "EUR"
    }); // These two events will have the same correlation ID
}
```
If you try to create two strict correlation handles it will throw in DEBUG with the debugger attached,
otherwise just record error and give you back the first handle.

__Lose__ correlation is when you are doing parallel processing and you want multiple correlation handles
active at the same time. To support this you can use anything that inherits from `object` as a handle.
```C#
IBigBrother bb = new BigBrother("[Application Insights Key]", "[Application Insights Key - For inner telemetry]");
var handle = new object();

bb.Publish(new PaymentEvent
{
    ProcessedOn = DateTime.Now,
    Ammount = 100,
    Currency = "USD"
}, handle); // These two events will have the same correlation ID

bb.Publish(new PaymentEvent
{
    ProcessedOn = DateTime.Now,
    Ammount = 200,
    Currency = "USD"
}, handle); // These two events will have the same correlation ID
```
By default correlation handles are kept for 10 minutes, but you can change this keep alive `TimeSpan` by calling
```C#
void SetCorrelationKeepAlive(TimeSpan span)
```
They will be released after this time and if you try to use the handle again you'll get a new correlation vector for it.

### What can I also do with it?

You can force a Flush of the AI client, which will send all events right away:
```c#
bb.Flush();
```

You can also set the AI client to use a channel in DeveloperMode, this will push events right away
without streaming them in memory. If you're getting a release package (from nuget.org), this doesn't do anything
this is to force deployments to never have channels in DeveloperMode even if developers forget to remove the code.
```c#
bb.DeveloperMode();
```

### Do I just include this package for instrumentation?

Depends on what time of application you're writting. This only includes the
[core Application Insights package](https://www.nuget.org/packages/Microsoft.ApplicationInsights/),
so if your application can leverage other packages it should also include any of the top level AI packages.
For example, web applications should also include the
[Application Insights for Web package](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Web/).

### What is this AI key for inner telemetry?

This is the Application Insights account where `BigBrother` pushes internal telemetry.
By design, `BigBrother` will never throw, but everytime an exception is raised,
it is stream to the inner telemetry account.
We also track certain events, to see if we have wrong usage of BigBrother,
for example we track all calls to `Flush`, so if you're pushing events and using `Flush` right after,
we know about it!

### I want to write some tests now, but I don't want to send events

`BigBrother` does a bit of heavy lifting on both it's constructor and the class static constructor,
so you should always stub `IBigBrother` instead, to avoid the heavy lifting done on the constructors.