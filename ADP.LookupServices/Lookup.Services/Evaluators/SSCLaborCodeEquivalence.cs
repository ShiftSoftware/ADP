using System;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// The equivalence classes built from <see cref="LookupOptions.SSCInterchangeableLaborCodeGroups"/>: for any labor
/// operation code, the set of codes a dealer system may have recorded instead of it. Groups that share a code are
/// merged, so equivalence is transitive; codes are compared trimmed and case-insensitively, because the feeds
/// that carry them are hand-maintained and a stray space or a lower-case entry must not hide a completed repair.
/// </summary>
public sealed class SSCLaborCodeEquivalence
{
    private static readonly SSCLaborCodeEquivalence EmptyInstance = new SSCLaborCodeEquivalence(null);

    private readonly Dictionary<string, HashSet<string>> classes;

    /// <summary>An equivalence with no groups: every code is equivalent only to itself.</summary>
    public static SSCLaborCodeEquivalence Empty => EmptyInstance;

    public SSCLaborCodeEquivalence(IEnumerable<IEnumerable<string>> groups)
    {
        classes = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var group in groups ?? Enumerable.Empty<IEnumerable<string>>())
        {
            var members = (group ?? Enumerable.Empty<string>())
                .Select(Normalize)
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();

            if (members.Count < 2)
                continue;

            // Union every class any member already belongs to with the new members, then point all of them at
            // the merged set so a code listed in two groups makes one transitive class rather than two.
            var merged = new HashSet<string>(members, StringComparer.Ordinal);
            foreach (var member in members)
                if (classes.TryGetValue(member, out var existing))
                    merged.UnionWith(existing);

            foreach (var member in merged)
                classes[member] = merged;
        }
    }

    /// <summary>Whether any interchangeable group is configured at all.</summary>
    public bool IsEmpty => classes.Count == 0;

    /// <summary>
    /// The codes interchangeable with <paramref name="laborCode"/>, excluding the code itself. Empty when the code
    /// belongs to no configured group.
    /// </summary>
    public IReadOnlyCollection<string> AlternativesFor(string laborCode)
    {
        var normalized = Normalize(laborCode);

        if (normalized.Length == 0 || !classes.TryGetValue(normalized, out var members))
            return Array.Empty<string>();

        return members.Where(m => m != normalized).OrderBy(m => m, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// The matching key for a labor operation code: trimmed and upper-cased. Null and blank codes map to the
    /// empty string, which never matches anything.
    /// </summary>
    public static string Normalize(string laborCode) =>
        string.IsNullOrWhiteSpace(laborCode) ? string.Empty : laborCode.Trim().ToUpperInvariant();
}
