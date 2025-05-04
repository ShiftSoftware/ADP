namespace ShiftSoftware.ADP.SyncAgent.ConfigurationModels;

public class DataProcessConfigurations<TData>
    where TData : class
{
    public int CurrentStep { get; private set; }
    public int TotalStep { get; private set; }
    public int TotalCount { get; private set; }
    public DataProcessActionType ActionType { get; private set; }
    public IEnumerable<TData> Items { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public SyncTaskStatus TaskStatus { get; private set; }

    public DataProcessConfigurations()
    {
        
    }

    internal DataProcessConfigurations(
        int currentStep,
        int totalStep,
        int totalCount,
        DataProcessActionType actionType,
        CancellationToken cancellationToken,
        IEnumerable<TData> items,
        SyncTaskStatus taskStatus)
    {
        CurrentStep = currentStep;
        TotalStep = totalStep;
        TotalCount = totalCount;
        ActionType = actionType;
        CancellationToken = cancellationToken;
        Items = items;
        TaskStatus = taskStatus;
    }
}
