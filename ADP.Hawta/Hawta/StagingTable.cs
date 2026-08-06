namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Handle to a staging table created by <see cref="SnapshotStore.CreateStagingTable"/>.
/// The internal constructor is deliberate: merge input can only come from store-created
/// staging, which makes the merge's SQL injection-proof by construction.
/// </summary>
public sealed class StagingTable
{
    /// <summary>Bare table name (e.g. <c>staging_Widget</c>).</summary>
    public string Name { get; }

    /// <summary>Schema-qualified, quoted reference (e.g. <c>stage."staging_Widget"</c> or <c>temp.main."staging_Widget"</c>).</summary>
    public string QualifiedName { get; }

    internal StagingTable(string name, string qualifiedName)
    {
        Name = name;
        QualifiedName = qualifiedName;
    }
}
