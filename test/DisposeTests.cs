namespace LostTech.Checkpoint
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Xunit;
    public class DisposeTests
    {
        [Fact]
        public async Task WaitsForCompletion()
        {
            int ops = 0;
            var chain = new AsyncChainService();
            for (int i = 0; i < 10; i++)
                chain.Chain(async () => {
                    await Task.Delay(100);
                    ops++;
                });
            var waitStopwatch = Stopwatch.StartNew();
            await chain.DisposeAsync();
            Debug.WriteLine($"disposed after {waitStopwatch.ElapsedMilliseconds}ms");
            Assert.Equal(10, ops);
        }
    }
}
