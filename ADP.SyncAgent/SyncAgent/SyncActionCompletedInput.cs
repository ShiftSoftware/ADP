namespace ShiftSoftware.ADP.SyncAgent;

public class SyncActionCompletedInput
{
    public SyncActionType ActionType { get; private set; }
    public bool Succeeded { get; private set; }

    public SyncActionCompletedInput(SyncActionType actionType, bool succeeded)
    {
        this.ActionType = actionType;
        this.Succeeded = succeeded;
    }
}
