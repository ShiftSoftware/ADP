namespace ShiftSoftware.ADP.SyncAgent;

public class SyncMappingInput<TSource, TDestination> where TSource : class where TDestination : class
{
    public IEnumerable<TSource?>? SourceItems { get; private set; }

    /// <summary>
    /// In retry this contains the previous mapped item of the same step,
    /// otherwise it will be null.
    /// </summary>
    public IEnumerable<TDestination?>? PreviousMappedItem { get; private set; }
    public SyncActionStatus Status { get; private set; }

    internal SyncMappingInput(
        IEnumerable<TSource?>? sourceItems,
        IEnumerable<TDestination?>? previousMappedItem,
        SyncActionStatus status)
    {
        this.SourceItems = sourceItems;
        this.PreviousMappedItem = previousMappedItem;
        this.Status = status;
    }
}