namespace ShiftSoftware.ADP.SyncAgent;

public class SyncCosmosAction<T> where T : class
{
    internal T? Item { get; set; }
    public CosmosActionType ActionType { get; set; }

    public Func<SyncCosmosActionMappingInput<T?>, ValueTask<T?>> Mapping { get; set; }

    public CancellationToken CancellationToken { get; private set; }

    public SyncCosmosAction()
    {

    }

    internal SyncCosmosAction(T? item, CosmosActionType actionType, CancellationToken cancellationToken)
    {
        Item = item;
        ActionType = actionType;
        CancellationToken = cancellationToken;
    }
}

public class SyncAgentCosmosAction<T> where T : class
{
    internal T? Item { get; set; }
    public DataProcessActionType ActionType { get; set; }

    public Func<SyncCosmosActionMappingInput<T?>, ValueTask<T?>> Mapping { get; set; }

    public CancellationToken CancellationToken { get; private set; }

    public SyncAgentCosmosAction()
    {

    }

    internal SyncAgentCosmosAction(T? item, DataProcessActionType actionType, CancellationToken cancellationToken)
    {
        Item = item;
        ActionType = actionType;
        CancellationToken = cancellationToken;
    }
}

public class SyncCosmosActionMappingInput<T> where T : class
{
    public T? Item { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    public SyncCosmosActionMappingInput()
    {

    }

    public SyncCosmosActionMappingInput(T? item, CancellationToken cancellationToken)
    {
        Item = item;
        CancellationToken = cancellationToken;
    }
}