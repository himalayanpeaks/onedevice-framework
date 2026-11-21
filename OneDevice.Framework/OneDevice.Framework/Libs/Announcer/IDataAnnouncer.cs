namespace OneDevice.Framework.Libs.Announcer;

/// <remarks>
///     <see cref="AttachToProcessDataEvent" /> is typed with <see cref="BaseDataForAnnouncement" /> instead of
///     <typeparamref name="TData" />, so a <see cref="DataTunnel{TEventArgs}" /> of a derived data type cannot implement
///     it. The signature is kept for binary compatibility and will be corrected in 3.0.
/// </remarks>
[Obsolete("Cannot be implemented for derived data types (AttachToProcessDataEvent is not typed with TData). " +
          "Use OneDevice.Module.Hal.IHalLayer<T> instead. Will be corrected in OneDevice.Framework 3.0.")]
public interface IDataAnnouncer<TData>
    where TData : BaseDataForAnnouncement
{
    public void StartProcessDataAnnouncer();
    public void StopProcessDataAnnouncer();

    public void AttachToProcessDataEvent(DataTunnel<BaseDataForAnnouncement>.DataEventHandler bla);
}
