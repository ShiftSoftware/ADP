namespace ADP.SyncAgent;

public class SyncCosmosAction<T> where T : class
{
    internal T? Item { get; set; }
    public CosmosActionType ActionType { get; set; }

    public Func<T?, ValueTask<T?>> Mapping { get; set; }

    public SyncCosmosAction()
    {

    }

    internal SyncCosmosAction(T? item, CosmosActionType actionType)
    {
        Item = item;
        ActionType = actionType;
    }
}
