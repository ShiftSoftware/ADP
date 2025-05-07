namespace ShiftSoftware.ADP.SyncAgent;

public class SyncFunctionInput<T> : SyncFunctionInput
{
    public T Input { get; private set; }

    public SyncFunctionInput(CancellationToken cancellationToken, T input) : base(cancellationToken)
    {
        Input = input;
    }
}

public class SyncFunctionInput
{
    public CancellationToken CancellationToken { get; private set; }

    public SyncFunctionInput(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }
}
