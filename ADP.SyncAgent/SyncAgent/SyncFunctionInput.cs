namespace ShiftSoftware.ADP.SyncAgent;

public class SyncFunctionInput<T> : SyncFunctionInput
{
    public T Input { get; private set; }
    
    internal SyncFunctionInput(CancellationToken cancellationToken, T input, IEnumerable<ISyncProgressIndicator2> syncProgressIndicators) : base(cancellationToken, syncProgressIndicators)
    {
        Input = input;
    }
}

public class SyncFunctionInput
{
    public CancellationToken CancellationToken { get; private set; }
    public IEnumerable<ISyncProgressIndicator2> SyncProgressIndicators { get; private set; } = [];

    internal SyncFunctionInput(CancellationToken cancellationToken, IEnumerable<ISyncProgressIndicator2> syncProgressIndicators)
    {
        CancellationToken = cancellationToken;
        SyncProgressIndicators = syncProgressIndicators;
    }
}
