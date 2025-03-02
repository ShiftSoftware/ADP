namespace ShiftSoftware.ADP.SyncAgent;

public class SyncTask
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string? SyncID { get; set; }
    public string TaskDescription { get; set; } = default!;
    public double Progress { get; set; }
    public int CurrentStep { get; set; }
    public int TotalStep { get; set; }
    public TimeSpan Elapsed { get; set; }
    public TimeSpan RemainingTimeToShutdown { get; set; }
    public bool Completed { get; set; }
    public bool Failed { get; set; }
    public int Sort { get; set; }
}