using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevOpsFlex.Telemetry;
using DevOpsFlex.Tests.Core;
using FluentAssertions;
using Xunit;

// ReSharper disable once CheckNamespace
public class SingleReplayCastTest
{
    [Fact, IsUnit]
    public async Task Test_SubscribeAfterEvents()
    {
        var reset = new ManualResetEventSlim();

        var sequence = new[] {1, 2, 3, 4, 5};
        var origin = new Subject<int>();

        var replay = new SingleReplayCast<int>(origin);

        foreach (var num in sequence)
        {
            origin.OnNext(num);
        }

        var result = new List<int>();

        using (replay.Subscribe(result.Add))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            result.Should().ContainInOrder(sequence);
        }
    }

    [Fact, IsUnit]
    public void Test_DisposeClosesProperly()
    {
        var connection = new Disposable();
        var subscription = new Disposable();

        var replay = new SingleReplayCast<int>(Observable.Empty<int>())
        {
            ReplayConnection = connection,
            ReplaySubscription = subscription
        };

        replay.Dispose();

        connection.IsDisposed.Should().BeTrue();
        subscription.IsDisposed.Should().BeTrue();

        replay.Replay.IsDisposed.Should().BeTrue();
    }
}

public class Disposable : IDisposable
{
    public bool IsDisposed;

    public void Dispose()
    {
        IsDisposed = true;
    }
}
