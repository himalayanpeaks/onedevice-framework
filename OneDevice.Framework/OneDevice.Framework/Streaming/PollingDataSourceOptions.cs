namespace OneDevice.Framework.Streaming;

/// <summary>Settings for <see cref="PollingDataSource{T}" />.</summary>
public sealed record PollingDataSourceOptions
{
    /// <summary>
    ///     Time between the start of two fetches. <see cref="TimeSpan.Zero" /> (default) polls back-to-back, which is
    ///     only appropriate when the fetch itself blocks on I/O.
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.Zero;

    /// <summary>Pause before the next attempt after a fetch threw.</summary>
    public TimeSpan ErrorRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     When true (default) a sample is only published if it differs from the previous one
    ///     (see the comparer passed to <see cref="PollingDataSource{T}" />; records compare by value).
    /// </summary>
    public bool PublishOnlyChanges { get; init; } = true;

    /// <summary>
    ///     Samples buffered per <see cref="PollingDataSource{T}.ReadAllAsync" /> reader. When a reader falls behind,
    ///     the oldest samples are dropped.
    /// </summary>
    public int ReaderBufferSize { get; init; } = 100;

    /// <summary>Maximum time <see cref="PollingDataSource{T}.StopAsync" /> waits for an in-flight fetch.</summary>
    public TimeSpan StopTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Name used in log messages and for the polling thread.</summary>
    public string Name { get; init; } = "PollingDataSource";
}
