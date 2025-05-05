namespace ShiftSoftware.ADP.SyncAgent;

public class SyncTaskStatus
{
    public string ID { get; internal set; } = Guid.NewGuid().ToString();
    public string? SyncID { get; internal set; }
    public string TaskDescription { get; internal set; } = default!;
    public double Progress { get; internal set; }
    public int CurrentStep { get; internal set; }
    public int TotalStep { get; internal set; }
    public TimeSpan Elapsed { get; internal set; }
    public TimeSpan RemainingTimeToShutdown { get; internal set; }
    public bool Completed { get; internal set; }
    public bool Failed { get; internal set; }
    public int Sort { get; internal set; }

    internal DateTime OperationStart { get; set; }
    internal int OperationTimeoutInSeconds { get; set; }

    public SyncTaskStatus UpdateProgress(bool incrementStep = true)
    {
        if (incrementStep)
            CurrentStep++;

        Progress = TotalStep == 0 ? 0 : (double)(CurrentStep) / TotalStep;
        Elapsed = DateTime.UtcNow - OperationStart;
        RemainingTimeToShutdown = OperationStart.AddSeconds(OperationTimeoutInSeconds) - DateTime.UtcNow;

        return this;
    }
}