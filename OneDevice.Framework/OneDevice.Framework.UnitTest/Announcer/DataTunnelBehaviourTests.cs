using OneDevice.Framework.Libs.Announcer;

namespace OneDevice.Framework.UnitTest.Announcer;

public class DataTunnelBehaviourTests
{
    private sealed class Sample : BaseDataForAnnouncement
    {
        public int Value { get; init; }
    }

    /// <summary>Produces a new sample on every fetch; can be told to throw.</summary>
    private sealed class CountingTunnel : DataTunnel<Sample>
    {
        private int _counter;
        public volatile bool ThrowOnFetch;
        public int Fetches;

        protected override TimeSpan PollInterval => TimeSpan.FromMilliseconds(5);
        protected override TimeSpan ErrorRetryDelay => TimeSpan.FromMilliseconds(10);

        protected override void FetchDataForTunnel(ref Sample data)
        {
            Interlocked.Increment(ref Fetches);
            if (ThrowOnFetch) throw new IOException("device unplugged");
            data = new Sample { Value = Interlocked.Increment(ref _counter) };
        }

        public void Start() => StartAnnouncingData();
        public void Stop() => StopAnnouncingData();
        public bool Running => IsAnnouncing;
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        return SpinWait.SpinUntil(condition, timeoutMs);
    }

    [Fact]
    public void Announces_NewData()
    {
        var tunnel = new CountingTunnel();
        var received = 0;
        tunnel.DataEvent += (_, _) => Interlocked.Increment(ref received);

        tunnel.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref received) >= 3));
        tunnel.Stop();
    }

    [Fact]
    public void Stop_WaitsForLoop_NoEventsAfterStopReturns()
    {
        var tunnel = new CountingTunnel();
        var received = 0;
        tunnel.DataEvent += (_, _) => Interlocked.Increment(ref received);
        tunnel.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref received) >= 1));

        tunnel.Stop();
        var afterStop = Volatile.Read(ref received);
        Thread.Sleep(50);

        Assert.False(tunnel.Running);
        Assert.Equal(afterStop, Volatile.Read(ref received));
    }

    [Fact]
    public void Exception_InFetch_IsReported_AndLoopKeepsRunning()
    {
        var tunnel = new CountingTunnel { ThrowOnFetch = true };
        Exception? reported = null;
        var received = 0;
        tunnel.AnnouncerError += (_, ex) => reported = ex;
        tunnel.DataEvent += (_, _) => Interlocked.Increment(ref received);

        tunnel.Start();
        Assert.True(WaitUntil(() => reported != null));
        Assert.IsType<IOException>(reported);

        tunnel.ThrowOnFetch = false; // device back
        Assert.True(WaitUntil(() => Volatile.Read(ref received) >= 1));
        Assert.True(tunnel.Running);
        tunnel.Stop();
    }

    [Fact]
    public void Exception_InSubscriber_DoesNotKillLoop()
    {
        var tunnel = new CountingTunnel();
        var calls = 0;
        tunnel.DataEvent += (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1) throw new InvalidOperationException("bad handler");
        };

        tunnel.Start();
        Assert.True(WaitUntil(() => Volatile.Read(ref calls) >= 3));
        tunnel.Stop();
    }

    [Fact]
    public void StopThenStart_Immediately_Restarts()
    {
        var tunnel = new CountingTunnel();
        var received = 0;
        tunnel.DataEvent += (_, _) => Interlocked.Increment(ref received);

        for (var i = 0; i < 20; i++)
        {
            tunnel.Start();
            tunnel.Stop();
        }

        tunnel.Start();
        var before = Volatile.Read(ref received);
        Assert.True(WaitUntil(() => Volatile.Read(ref received) > before));
        tunnel.Stop();
    }

    [Fact]
    public void Stop_FromDataEventHandler_DoesNotDeadlock()
    {
        var tunnel = new CountingTunnel();
        var stopped = new ManualResetEventSlim();
        tunnel.DataEvent += (_, _) =>
        {
            tunnel.Stop();
            stopped.Set();
        };

        tunnel.Start();
        Assert.True(stopped.Wait(2000));
        Assert.True(WaitUntil(() => !tunnel.Running));
    }

    [Fact]
    public void Start_Twice_RunsSingleLoop()
    {
        var tunnel = new CountingTunnel();
        tunnel.Start();
        tunnel.Start();
        Assert.True(tunnel.Running);
        tunnel.Stop();
        Assert.False(tunnel.Running);
    }
}
