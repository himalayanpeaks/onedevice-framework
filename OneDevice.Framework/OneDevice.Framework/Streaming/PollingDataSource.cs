using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OneDevice.Framework.Streaming;

/// <summary>
///     Polls a fetch function on a dedicated background thread and publishes new samples, both as an event
///     (<see cref="DataReceived" />) and as an async stream (<see cref="ReadAllAsync" />).
/// </summary>
/// <remarks>
///     <para>
///         Use by composition instead of inheriting from <c>DataTunnel</c>:
///         <code>
/// _source = new PollingDataSource&lt;Reading&gt;(ct =&gt; ValueTask.FromResult(ReadFromPort()),
///     new PollingDataSourceOptions { Interval = TimeSpan.FromMilliseconds(500) }, logger);
/// _source.Start();
/// await foreach (var reading in _source.ReadAllAsync(ct)) { ... }
///         </code>
///     </para>
///     <para>
///         The fetch function may block (serial I/O): it runs on its own thread. Returning <c>null</c> means
///         "no new sample". Exceptions from the fetch or from <see cref="DataReceived" /> handlers are logged,
///         reported through <see cref="Error" /> and do not stop polling.
///     </para>
///     <para><see cref="DataReceived" /> and <see cref="Error" /> are raised on the polling thread.</para>
/// </remarks>
public sealed class PollingDataSource<T> : IAsyncDisposable, IDisposable where T : class
{
    [ThreadStatic] private static PollingDataSource<T>? _pollingOnCurrentThread;

    private readonly IEqualityComparer<T> _comparer;
    private readonly Func<CancellationToken, ValueTask<T?>> _fetch;
    private readonly ILogger _logger;
    private readonly List<Channel<T>> _readers = new();
    private readonly object _sync = new();
    private CancellationTokenSource? _cancellation;
    private bool _disposed;
    private T? _latest;
    private Task? _pollingTask;

    /// <param name="fetch">Reads one sample; return null when there is nothing new.</param>
    /// <param name="options">Polling settings; defaults are used when null.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="comparer">Decides whether a sample changed; defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public PollingDataSource(Func<CancellationToken, ValueTask<T?>> fetch, PollingDataSourceOptions? options = null,
        ILogger? logger = null, IEqualityComparer<T>? comparer = null)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
        Options = options ?? new PollingDataSourceOptions();
        if (Options.ReaderBufferSize < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "ReaderBufferSize must be at least 1.");
        _logger = logger ?? NullLogger.Instance;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>Convenience overload for a synchronous (blocking) fetch.</summary>
    public PollingDataSource(Func<T?> fetch, PollingDataSourceOptions? options = null, ILogger? logger = null,
        IEqualityComparer<T>? comparer = null)
        : this(WrapSync(fetch), options, logger, comparer)
    {
    }

    public PollingDataSourceOptions Options { get; }

    /// <summary>Most recently published sample, or null.</summary>
    public T? Latest => Volatile.Read(ref _latest);

    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _pollingTask is { IsCompleted: false };
            }
        }
    }

    /// <summary>Raised on the polling thread for every published sample.</summary>
    public event EventHandler<T>? DataReceived;

    /// <summary>Raised when the fetch or a <see cref="DataReceived" /> handler throws. Polling continues.</summary>
    public event EventHandler<Exception>? Error;

    /// <summary>Starts polling. Does nothing if already running.</summary>
    public void Start()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_pollingTask is { IsCompleted: false }) return;

            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _pollingTask = Task.Factory.StartNew(() => Poll(token), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    /// <summary>
    ///     Stops polling and waits (up to <see cref="PollingDataSourceOptions.StopTimeout" />) for the current fetch to
    ///     finish, so no sample is published after this completes. When called from a <see cref="DataReceived" />
    ///     handler it only requests the stop.
    /// </summary>
    public async Task StopAsync()
    {
        Task task;
        CancellationTokenSource cancellation;
        lock (_sync)
        {
            if (_pollingTask is null || _cancellation is null) return;
            task = _pollingTask;
            cancellation = _cancellation;
            _pollingTask = null;
            _cancellation = null;
            cancellation.Cancel();
        }

        if (ReferenceEquals(_pollingOnCurrentThread, this)) return;

        var finished = await Task.WhenAny(task, Task.Delay(Options.StopTimeout)).ConfigureAwait(false) == task;
        if (finished)
            cancellation.Dispose();
        else
            _logger.LogWarning("{Name}: polling did not stop within {Timeout}", Options.Name, Options.StopTimeout);
    }

    /// <summary>
    ///     Streams published samples until <paramref name="cancellationToken" /> is cancelled or the source is disposed.
    ///     Each call gets its own buffer; it starts with the samples published after the call.
    /// </summary>
    public async IAsyncEnumerable<T> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(Options.ReaderBufferSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _readers.Add(channel);
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            lock (_sync)
            {
                _readers.Remove(channel);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var reader in _readers) reader.Writer.TryComplete();
            _readers.Clear();
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void Poll(CancellationToken token)
    {
        _pollingOnCurrentThread = this;
        Thread.CurrentThread.Name ??= Options.Name;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var started = DateTime.UtcNow;
                try
                {
                    // Dedicated thread without a synchronization context: blocking on the ValueTask is safe here.
                    var sample = _fetch(token).AsTask().GetAwaiter().GetResult();
                    if (sample != null && !token.IsCancellationRequested) Publish(sample);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Name}: fetch failed, retrying in {Delay}", Options.Name, Options.ErrorRetryDelay);
                    RaiseError(ex);
                    if (token.WaitHandle.WaitOne(Options.ErrorRetryDelay)) break;
                    continue;
                }

                var remaining = Options.Interval - (DateTime.UtcNow - started);
                if (remaining > TimeSpan.Zero && token.WaitHandle.WaitOne(remaining)) break;
            }
        }
        finally
        {
            _pollingOnCurrentThread = null;
        }
    }

    private void Publish(T sample)
    {
        var previous = Volatile.Read(ref _latest);
        if (Options.PublishOnlyChanges && previous != null && _comparer.Equals(previous, sample)) return;
        Volatile.Write(ref _latest, sample);

        lock (_sync)
        {
            foreach (var reader in _readers) reader.Writer.TryWrite(sample);
        }

        var handlers = DataReceived;
        if (handlers == null) return;
        foreach (EventHandler<T> handler in handlers.GetInvocationList())
            try
            {
                handler(this, sample);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Name}: DataReceived handler threw", Options.Name);
                RaiseError(ex);
            }
    }

    private void RaiseError(Exception ex)
    {
        try
        {
            Error?.Invoke(this, ex);
        }
        catch (Exception handlerEx)
        {
            _logger.LogError(handlerEx, "{Name}: Error handler threw", Options.Name);
        }
    }

    private static Func<CancellationToken, ValueTask<T?>> WrapSync(Func<T?> fetch)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        return _ => ValueTask.FromResult(fetch());
    }
}
