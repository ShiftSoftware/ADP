namespace ShiftSoftware.ADP.SyncAgent;

public class SyncFunctionInput<T> : SyncFunctionInput
{
    public T Input { get; private set; }
    
    internal SyncFunctionInput(CancellationToken cancellationToken, T input, IEnumerable<ISyncEngineLogger> syncProgressIndicators) : base(cancellationToken, syncProgressIndicators)
    {
        Input = input;
    }
}

public class SyncFunctionInput
{
    public CancellationToken CancellationToken { get; private set; }
    public IEnumerable<ISyncEngineLogger> SyncProgressIndicators { get; private set; } = [];

    internal SyncFunctionInput(CancellationToken cancellationToken, IEnumerable<ISyncEngineLogger> syncProgressIndicators)
    {
        CancellationToken = cancellationToken;
        SyncProgressIndicators = syncProgressIndicators;
    }
}
