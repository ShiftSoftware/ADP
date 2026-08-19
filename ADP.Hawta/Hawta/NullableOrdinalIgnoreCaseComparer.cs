namespace ShiftSoftware.ADP.Hawta;

/// <summary>Ordinal-ignore-case equality for nullable source scopes.</summary>
internal sealed class NullableOrdinalIgnoreCaseComparer : IEqualityComparer<string?>
{
    public static readonly NullableOrdinalIgnoreCaseComparer Instance = new();

    private NullableOrdinalIgnoreCaseComparer() { }

    public bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

    public int GetHashCode(string? obj) => obj is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
}
