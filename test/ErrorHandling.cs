namespace LostTech.Checkpoint;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Xunit;
public class ErrorHandling {
    [Fact]
    public async Task AggregatesErrors() {
        var chain = new AsyncChainService();
        for (int i = 0; i < 10; i++)
            chain.Chain(async () => {
                await Task.Delay(100);
                throw new ArgumentOutOfRangeException(nameof(i), i, "test error");
            });
        await chain.DisposeAsync();
        Assert.NotNull(chain.FatalErrors);
        Assert.Equal(10, chain.FatalErrors.InnerExceptions.Count);
    }

    [Fact]
    public async Task Observing() {
        var chain = new AsyncChainService();
        int observed = 0;
        chain.TaskException += (_, e) => {
            if (e.Exception.InnerException is ArgumentOutOfRangeException) {
                e.SetObserved();
                observed++;
            }
        };
        for (int i = 0; i < 10; i++)
            chain.Chain(async () => {
                await Task.Delay(100);
                throw new ArgumentOutOfRangeException(nameof(i), i, "test error");
            });
        var waitStopwatch = Stopwatch.StartNew();
        await chain.DisposeAsync();
        Assert.Null(chain.FatalErrors);
        Assert.Equal(10, observed);
    }
}
