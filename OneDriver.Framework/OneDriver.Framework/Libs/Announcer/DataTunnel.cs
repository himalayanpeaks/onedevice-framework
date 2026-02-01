namespace OneDriver.Framework.Libs.Announcer;

public abstract class DataTunnel<TEventArgs> where TEventArgs : BaseDataForAnnouncement, new()
{
    public delegate void DataEventHandler(object sender, TEventArgs e);

    protected DataTunnel()
    {
        DataTask = new Task(Announcer);
        AnnouncerCancellationTokenSource = new CancellationTokenSource();
        AnnouncerCancellationToken = AnnouncerCancellationTokenSource.Token;
    }

    private CancellationToken AnnouncerCancellationToken { get; set; }
    private CancellationTokenSource AnnouncerCancellationTokenSource { get; set; }
    private Task DataTask { get; set; }
    private TEventArgs? PreviousData { get; set; }
    public event DataEventHandler? DataEvent;

    private void Announcer()
    {
        var newData = new TEventArgs();
        var PreviousData = new TEventArgs();
        while (!AnnouncerCancellationToken.IsCancellationRequested)
        {
            FetchDataForTunnel(ref newData);
            if (!newData.Equals(PreviousData))
            {
                DataEvent?.Invoke(this, newData);
                PreviousData = newData;
            }
        }

        AnnouncerCancellationTokenSource.Dispose();
    }

    protected void StartAnnouncingData()
    {
        if (DataTask.Status == TaskStatus.Running)
            return;
        DataTask = new Task(Announcer);
        AnnouncerCancellationTokenSource = new CancellationTokenSource();
        AnnouncerCancellationToken = AnnouncerCancellationTokenSource.Token;
        DataTask.Start();
    }

    protected void StopAnnouncingData()
    {
        if (DataTask.Status == TaskStatus.Running) AnnouncerCancellationTokenSource.Cancel();
    }

    protected abstract void FetchDataForTunnel(ref TEventArgs data);
}