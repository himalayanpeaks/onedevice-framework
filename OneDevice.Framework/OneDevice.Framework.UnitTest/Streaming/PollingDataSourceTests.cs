using OneDevice.Framework.Streaming;

namespace OneDevice.Framework.UnitTest.Streaming;

public class PollingDataSourceTests
{
    private sealed record Reading(int Channel, double Volts);

    private static readonly PollingDataSourceOptions Fast = new()
    {
        Interval = TimeSpan.FromMilliseconds(5),
        ErrorRetryDelay = TimeSpan.FromMilliseconds(10)
    };

    private static bool WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        return SpinWait.SpinUntil(condition, timeoutMs);
    }

    [Fact]
    public async Task PublishesOnlyChanges_ByValue()
    {
        var volts = 1.0;
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, Volatile.Read(ref volts)), Fast);
        var received = new List<Reading>();
        source.DataReceived += (_, r) => { lock (received) received.Add(r); };

        source.Start();
        Assert.True(WaitUntil(() => { lock (received) return received.Count == 1; }));
        Thread.Sleep(50); // many polls of an unchanged (but new) record instance
        lock (received) Assert.Single(received);

        Volatile.Write(ref volts, 2.0);
        Assert.True(WaitUntil(() => { lock (received) return received.Count == 2; }));
        Assert.Equal(new Reading(0, 2.0), source.Latest);
    }

    [Fact]
    public async Task PublishOnlyChanges_False_PublishesEverySample()
    {
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, 1),
            Fast with { PublishOnlyChanges = false });
        var count = 0;
        source.DataReceived += (_, _) => Interlocked.Increment(ref count);
        source.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref count) >= 3));
    }

    [Fact]
    public async Task NullSample_IsNotPublished()
    {
        await using var source = new PollingDataSource<Reading>(() => null, Fast);
        var published = false;
        source.DataReceived += (_, _) => published = true;
        source.Start();
        Thread.Sleep(50);
        Assert.False(published);
        Assert.Null(source.Latest);
    }

    [Fact]
    public async Task ReadAllAsync_StreamsSamples()
    {
        var n = 0;
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, Interlocked.Increment(ref n)), Fast);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var collected = new List<Reading>();
        var reader = Task.Run(async () =>
        {
            await foreach (var r in source.ReadAllAsync(cts.Token))
            {
                collected.Add(r);
                if (collected.Count == 3) break;
            }
        });
        Assert.True(WaitUntil(() => true, 20));
        source.Start();
        await reader;

        Assert.Equal(3, collected.Count);
        Assert.True(collected[0].Volts < collected[1].Volts && collected[1].Volts < collected[2].Volts);
    }

    [Fact]
    public async Task FetchException_IsReported_AndPollingContinues()
    {
        var fail = true;
        await using var source = new PollingDataSource<Reading>(() =>
        {
            if (Volatile.Read(ref fail)) throw new IOException("port closed");
            return new Reading(0, 5);
        }, Fast);
        Exception? error = null;
        Reading? last = null;
        source.Error += (_, e) => error = e;
        source.DataReceived += (_, r) => last = r;

        source.Start();
        Assert.True(WaitUntil(() => error is IOException));
        Volatile.Write(ref fail, false);
        Assert.True(WaitUntil(() => last != null));
        Assert.True(source.IsRunning);
    }

    [Fact]
    public async Task ThrowingSubscriber_DoesNotAffectOthers()
    {
        var n = 0;
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, Interlocked.Increment(ref n)), Fast);
        var good = 0;
        source.DataReceived += (_, _) => throw new InvalidOperationException("bad subscriber");
        source.DataReceived += (_, _) => Interlocked.Increment(ref good);
        source.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref good) >= 3));
    }

    [Fact]
    public async Task StopAsync_NoSamplesAfterwards_AndCanRestart()
    {
        var n = 0;
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, Interlocked.Increment(ref n)), Fast);
        var count = 0;
        source.DataReceived += (_, _) => Interlocked.Increment(ref count);

        source.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref count) >= 1));
        await source.StopAsync();
        var afterStop = Volatile.Read(ref count);
        Thread.Sleep(50);
        Assert.Equal(afterStop, Volatile.Read(ref count));
        Assert.False(source.IsRunning);

        source.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref count) > afterStop));
    }

    [Fact]
    public async Task StopAsync_FromHandler_DoesNotDeadlock()
    {
        await using var source = new PollingDataSource<Reading>(() => new Reading(0, 1), Fast);
        var stopped = new TaskCompletionSource();
        source.DataReceived += (_, _) =>
        {
            source.StopAsync().GetAwaiter().GetResult();
            stopped.TrySetResult();
        };
        source.Start();
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(WaitUntil(() => !source.IsRunning));
    }

    [Fact]
    public async Task AsyncFetch_IsSupported_AndCancelledOnStop()
    {
        var source = new PollingDataSource<Reading>(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct); // e.g. awaiting a device that never answers
            return null;
        }, Fast);
        source.Start();
        Thread.Sleep(20);
        var stop = source.StopAsync();
        Assert.Same(stop, await Task.WhenAny(stop, Task.Delay(2000)));
        await source.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_CompletesReaders()
    {
        var source = new PollingDataSource<Reading>(() => new Reading(0, 1), Fast);
        var enumeration = Task.Run(async () =>
        {
            await foreach (var _ in source.ReadAllAsync()) { }
        });
        Thread.Sleep(20);
        await source.DisposeAsync();
        Assert.Same(enumeration, await Task.WhenAny(enumeration, Task.Delay(2000)));
        Assert.Throws<ObjectDisposedException>(() => source.Start());
    }
}
