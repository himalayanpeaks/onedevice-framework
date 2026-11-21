using Serilog;

namespace OneDevice.Framework.Libs.Announcer;

/// <summary>
///     Runs a background loop that repeatedly calls <see cref="FetchDataForTunnel" /> and raises
///     <see cref="DataEvent" /> whenever new data was produced.
/// </summary>
/// <remarks>
///     <para>
///         Change detection uses <see cref="object.Equals(object?)" /> on <typeparamref name="TEventArgs" />.
///         Unless the data type overrides <c>Equals</c>, this is reference equality: assign a <b>new instance</b>
///         to <c>data</c> in <see cref="FetchDataForTunnel" /> when the data changed, and leave it untouched
///         when nothing changed. Do not mutate the previously announced instance.
///     </para>
///     <para>
///         <see cref="DataEvent" /> and <see cref="AnnouncerError" /> are raised on the background thread.
///     </para>
/// </remarks>
public abstract class DataTunnel<TEventArgs> where TEventArgs : BaseDataForAnnouncement, new()
{
    public delegate void DataEventHandler(object sender, TEventArgs e);

    // Identifies the announcer loop running on the current thread, so StopAnnouncingData can avoid
    // waiting for itself when it is called from a DataEvent handler (which would deadlock).
    [ThreadStatic] private static DataTunnel<TEventArgs>? _announcerOnCurrentThread;

    private readonly object _sync = new();
    private CancellationTokenSource? _cancellation;
    private Task? _announcerTask;

    public event DataEventHandler? DataEvent;

    /// <summary>
    ///     Raised when <see cref="FetchDataForTunnel" /> or a <see cref="DataEvent" /> subscriber throws.
    ///     The announcer keeps running and retries after <see cref="ErrorRetryDelay" />.
    /// </summary>
    public event EventHandler<Exception>? AnnouncerError;

    /// <summary>
    ///     Pause between two fetches. Default <see cref="TimeSpan.Zero" />: no pause, so
    ///     <see cref="FetchDataForTunnel" /> is expected to block on I/O. Override when fetching is fast.
    /// </summary>
    protected virtual TimeSpan PollInterval => TimeSpan.Zero;

    /// <summary>Pause before retrying after an exception in the loop.</summary>
    protected virtual TimeSpan ErrorRetryDelay => TimeSpan.FromSeconds(1);

    /// <summary>Maximum time <see cref="StopAnnouncingData" /> waits for the loop to finish.</summary>
    protected virtual TimeSpan StopTimeout => TimeSpan.FromSeconds(5);

    /// <summary>True while the announcer loop is running.</summary>
    protected bool IsAnnouncing
    {
        get
        {
            lock (_sync)
            {
                return _announcerTask is { IsCompleted: false };
            }
        }
    }

    protected void StartAnnouncingData()
    {
        lock (_sync)
        {
            if (_announcerTask is { IsCompleted: false })
                return;

            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            // LongRunning: the loop blocks for its whole lifetime, so give it a dedicated thread
            // instead of occupying a thread-pool thread.
            _announcerTask = Task.Factory.StartNew(() => Announcer(token), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    /// <summary>
    ///     Stops the loop and waits (up to <see cref="StopTimeout" />) until it has finished, so no
    ///     <see cref="DataEvent" /> is raised after this method returns. When called from a
    ///     <see cref="DataEvent" /> handler it only requests the stop and returns immediately.
    /// </summary>
    protected void StopAnnouncingData()
    {
        Task task;
        CancellationTokenSource cancellation;
        lock (_sync)
        {
            if (_announcerTask is null || _cancellation is null)
                return;

            task = _announcerTask;
            cancellation = _cancellation;
            _announcerTask = null;
            _cancellation = null;
            cancellation.Cancel();
        }

        if (ReferenceEquals(_announcerOnCurrentThread, this))
            return; // called from inside the loop: cannot wait for ourselves

        if (task.Wait(StopTimeout))
            cancellation.Dispose();
        else
            Log.Warning("{Tunnel}: announcer did not stop within {Timeout}", GetType().Name, StopTimeout);
    }

    protected abstract void FetchDataForTunnel(ref TEventArgs data);

    private void Announcer(CancellationToken token)
    {
        _announcerOnCurrentThread = this;
        try
        {
            var newData = new TEventArgs();
            var previousData = new TEventArgs();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    FetchDataForTunnel(ref newData);
                    if (!newData.Equals(previousData))
                    {
                        previousData = newData;
                        DataEvent?.Invoke(this, newData);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{Tunnel}: error in data announcer, retrying in {Delay}", GetType().Name,
                        ErrorRetryDelay);
                    RaiseAnnouncerError(ex);
                    if (token.WaitHandle.WaitOne(ErrorRetryDelay))
                        break;
                    continue;
                }

                if (PollInterval > TimeSpan.Zero && token.WaitHandle.WaitOne(PollInterval))
                    break;
            }
        }
        finally
        {
            _announcerOnCurrentThread = null;
        }
    }

    private void RaiseAnnouncerError(Exception ex)
    {
        try
        {
            AnnouncerError?.Invoke(this, ex);
        }
        catch (Exception handlerEx)
        {
            Log.Error(handlerEx, "{Tunnel}: AnnouncerError handler threw", GetType().Name);
        }
    }
}
