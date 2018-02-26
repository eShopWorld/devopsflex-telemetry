using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
public class SingleReplayCastTest
{
    [Fact, IsUnit]
    public async Task Test_SubscribeAfterEvents()
    {
        var sequence = new[] { 1, 2, 3, 4, 5 };
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
            replay.Replay.IsDisposed.Should().BeTrue();
        }
    }

    [Fact, IsUnit]
    public void Test_DisposeClosesProperly()
    {
        var connection = new Mock<IDisposable>();
        connection.Setup(x => x.Dispose()).Verifiable();

        var subscription = new Mock<IDisposable>();
        subscription.Setup(x => x.Dispose()).Verifiable();

        var replay = new SingleReplayCast<int>(Observable.Empty<int>())
        {
            ReplayConnection = connection.Object,
            ReplaySubscription = subscription.Object
        };

        replay.Dispose();

        connection.Verify();
        subscription.Verify();
        replay.Replay.IsDisposed.Should().BeTrue();
    }
}