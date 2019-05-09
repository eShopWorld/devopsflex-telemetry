using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Eshopworld.Telemetry;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Xunit;

public class StackTraceHelperTest
{
    [Fact, IsUnit]
    public void StackSimplificationIsAvailable()
    {
        StackTraceHelper.IsStackSimplificationAvailable.Should().BeTrue();
    }

    [Fact, IsUnit]
    public void StackIsSimplified()
    {
        Func<Task> call = OuterMethodAsync;
        Exception ex = call.Should().Throw<InvalidOperationException>().Which;

        var originalStack = new StackTrace(ex).GetFrames();
        var simplifiedStack = StackTraceHelper.SimplifyStackTrace(ex);
        simplifiedStack.Count().Should().BeLessThan(originalStack.Length);
    }


    private async Task OuterMethodAsync()
    {
        await InnerMethodAsync();
    }

    private async Task InnerMethodAsync()
    {
        await Task.Yield();

        throw new InvalidOperationException("test");
    }
}
