namespace ShiftSoftware.ADP.Cases.Data.Flow;

/// <summary>A case whose history is kept as rows of <typeparamref name="THistory"/>.</summary>
public interface ICaseWithHistory<THistory>
{
    /// <summary>Every status change and every note. Rows are only ever added.</summary>
    ICollection<THistory> StatusHistory { get; }
}
