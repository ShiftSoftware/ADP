using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// The sticky attribute overrides the engine honours on a resolve: loaded from
/// <c>Darlastic.StewardDecision</c> (Active rows of kind <see cref="StewardVerdict.Override"/>) and applied
/// when a customer's survived values are computed (<see cref="Merge.ApplyOverrides"/>).
///
/// <para>An override is anchored on a source record, not on a customer number: numbers change hands in a
/// merge, records do not. The customer whose records include the anchor gets the value. Where a customer's
/// records carry several overrides for one attribute (two customers with overrides were merged, or an
/// older row was left Active), the newest wins, and among equally new ones the highest decision ID: the
/// last thing a person said. An anchor this run did not load simply does not apply.</para>
///
/// <para>Overrides change what a customer SHOWS. They never change which records are one customer: matching
/// reads each record's own values, never the survived ones.</para>
/// </summary>
public sealed class StewardOverrides
{
    /// <summary>The empty set: a resolve with no overrides stages exactly what it did before they existed.</summary>
    public static readonly StewardOverrides None = new();

    private readonly Dictionary<(string System, string Id), List<(string Attr, string Value, DateTime AtUtc, long DecisionId)>> _byRecord = new();

    public bool IsEmpty => _byRecord.Count == 0;

    /// <summary>How many override rows were loaded.</summary>
    public int Count { get; private set; }

    /// <summary>Add one Active override row. An unknown attribute or an empty value is ignored.</summary>
    public void Add(string sourceSystem, string sourceRecordId, string attrType, string value, DateTime atUtc, long decisionId)
    {
        if (!GoldenAttribute.IsKnown(attrType) || string.IsNullOrWhiteSpace(value)) return;
        var key = (sourceSystem, sourceRecordId);
        (_byRecord.TryGetValue(key, out var list) ? list : _byRecord[key] = []).Add((attrType, value, atUtc, decisionId));
        Count++;
    }

    /// <summary>The overrides that apply to a customer made of <paramref name="members"/>: one value per attribute
    /// (the newest), in attribute order. Empty for nearly every customer.</summary>
    public IReadOnlyList<(string Attr, string Value)> For(IEnumerable<RealRecord> members)
    {
        if (_byRecord.Count == 0) return [];
        Dictionary<string, (string Value, DateTime AtUtc, long DecisionId)>? best = null;
        foreach (var m in members)
        {
            if (!_byRecord.TryGetValue((m.SourceSystem, m.SourceRecordId), out var list)) continue;
            best ??= new(StringComparer.Ordinal);
            foreach (var o in list)
                if (!best.TryGetValue(o.Attr, out var have) || o.AtUtc > have.AtUtc || (o.AtUtc == have.AtUtc && o.DecisionId > have.DecisionId))
                    best[o.Attr] = (o.Value, o.AtUtc, o.DecisionId);
        }
        return best is null ? [] : [.. best.OrderBy(b => b.Key, StringComparer.Ordinal).Select(b => (b.Key, b.Value.Value))];
    }
}
