namespace ShiftSoftware.ADP.SyncAgent;

/// <summary>
/// Marks a model property so it is excluded from the default row hash computation.
/// Properties from <see cref="SyncCsvBase"/> are always excluded automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreInHashAttribute : Attribute
{
}
