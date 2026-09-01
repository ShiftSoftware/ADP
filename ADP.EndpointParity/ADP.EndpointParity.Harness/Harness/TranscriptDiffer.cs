using System.Text;
using System.Text.Json.Nodes;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER: HttpClient, System.Text.Json and string only.

/// <summary>One structural difference between a baseline transcript and a replayed one.</summary>
public sealed record TranscriptDifference(string Path, string Kind, string? Baseline, string? Actual)
{
    public override string ToString() => Kind switch
    {
        "missing" => Path + ": present in baseline, ABSENT now  (baseline: " + Short(Baseline) + ")",
        "added"   => Path + ": ABSENT in baseline, present now  (now: " + Short(Actual) + ")",
        "type"    => Path + ": type changed  " + Short(Baseline) + "  ->  " + Short(Actual),
        _         => Path + ":  " + Short(Baseline) + "  ->  " + Short(Actual),
    };

    private static string Short(string? s)
    {
        if (s is null) return "<absent>";
        s = s.Replace("\n", " ");
        return s.Length <= 160 ? s : s.Substring(0, 157) + "...";
    }
}

/// <summary>
/// Structural diff over canonical JSON.
///
/// <para>
/// Compares VALUES, not schemas. verification.md section 1: every regression this upgrade can
/// produce returns 200 with a well-formed body of the correct shape, so a differ that reported
/// only shape changes would report nothing at all for all four traps. The unit of comparison
/// is the full normalized body.
/// </para>
///
/// <para>
/// Two properties this differ deliberately keeps, both from Rule 5:
/// a JSON <c>null</c> and an absent property are DIFFERENT (reported as a value change against
/// <c>&lt;absent&gt;</c>, not silently equal); and scalars are compared as their canonical TEXT,
/// so <c>1.0</c> and <c>1</c> on a decimal field are a difference rather than an equality.
/// </para>
/// </summary>
public static class TranscriptDiffer
{
    /// <summary>Diffs two golden texts. An unparseable side is reported as a whole-body change.</summary>
    public static IReadOnlyList<TranscriptDifference> Diff(string baselineText, string actualText)
    {
        var baseline = Canonical.TryParse(baselineText);
        var actual = Canonical.TryParse(actualText);

        if (baseline is null || actual is null)
        {
            // One side is not JSON at all (a text/csv export, or a corrupt golden). Compare as
            // text so the case still fails loudly rather than being skipped.
            return string.Equals(baselineText, actualText, StringComparison.Ordinal)
                ? Array.Empty<TranscriptDifference>()
                : new[] { new TranscriptDifference("$", "value", baselineText, actualText) };
        }

        var diffs = new List<TranscriptDifference>();
        Walk("$", baseline, actual, diffs);
        return diffs;
    }

    private static void Walk(string path, JsonNode? baseline, JsonNode? actual, List<TranscriptDifference> diffs)
    {
        // Rule 5: null and absent are distinct, and both are distinct from a value.
        if (baseline is null && actual is null) return;

        if (baseline is null)
        {
            diffs.Add(new TranscriptDifference(path, "added", null, Render(actual)));
            return;
        }

        if (actual is null)
        {
            diffs.Add(new TranscriptDifference(path, "missing", Render(baseline), null));
            return;
        }

        switch (baseline, actual)
        {
            case (JsonObject b, JsonObject a):
            {
                foreach (var key in b.Select(p => p.Key).Union(a.Select(p => p.Key)).OrderBy(k => k, StringComparer.Ordinal))
                {
                    b.TryGetPropertyValue(key, out var bv);
                    a.TryGetPropertyValue(key, out var av);

                    var hasB = b.ContainsKey(key);
                    var hasA = a.ContainsKey(key);

                    if (hasB && !hasA)
                    {
                        diffs.Add(new TranscriptDifference(path + "." + key, "missing", Render(bv), null));
                        continue;
                    }
                    if (!hasB && hasA)
                    {
                        diffs.Add(new TranscriptDifference(path + "." + key, "added", null, Render(av)));
                        continue;
                    }

                    Walk(path + "." + key, bv, av, diffs);
                }
                return;
            }

            case (JsonArray b, JsonArray a):
            {
                // A changed element COUNT is reported once, at the array, and then the shared
                // prefix is still compared element-by-element. Trap 1 turns [] into
                // [{...soft-deleted...}], so the count line is the headline and the element
                // diff is the evidence.
                if (b.Count != a.Count)
                    diffs.Add(new TranscriptDifference(path, "value",
                        "<array of " + b.Count + ">", "<array of " + a.Count + ">"));

                var shared = Math.Min(b.Count, a.Count);
                for (var i = 0; i < shared; i++)
                    Walk(path + "[" + i + "]", b[i], a[i], diffs);

                for (var i = shared; i < b.Count; i++)
                    diffs.Add(new TranscriptDifference(path + "[" + i + "]", "missing", Render(b[i]), null));
                for (var i = shared; i < a.Count; i++)
                    diffs.Add(new TranscriptDifference(path + "[" + i + "]", "added", null, Render(a[i])));

                return;
            }

            case (JsonObject, _):
            case (JsonArray, _):
            case (_, JsonObject):
            case (_, JsonArray):
                diffs.Add(new TranscriptDifference(path, "type", Render(baseline), Render(actual)));
                return;

            default:
            {
                // Scalars compare as canonical TEXT (Rule 5): 1.0 vs 1.00 vs 1 on a decimal?
                // money field is a real serialization change worth seeing on financial DTOs.
                var bt = Render(baseline);
                var at = Render(actual);
                if (!string.Equals(bt, at, StringComparison.Ordinal))
                    diffs.Add(new TranscriptDifference(path, "value", bt, at));
                return;
            }
        }
    }

    private static string Render(JsonNode? n) => n is null ? "<absent>" : Canonical.Write(n).Trim();

    /// <summary>
    /// Renders a whole run's diffs as the markdown report the operator reads.
    ///
    /// <para>
    /// <paramref name="comparedCount"/> is the number of cases the run actually compared, and it
    /// MUST be supplied by the caller rather than inferred from <paramref name="byCase"/>. The
    /// caller only records cases that differ, so a header derived from the dictionary would report
    /// "cases compared" equal to "cases with differences" every time - which reads as a collapse in
    /// coverage. Observed for real: a Surveys run comparing thirty cases with three differing
    /// rendered as "Cases compared: 3", and the honest reading of that line is that twenty-seven
    /// cases had silently stopped being checked.
    /// </para>
    /// </summary>
    public static string Report(string group, string grant, IReadOnlyDictionary<string, IReadOnlyList<TranscriptDifference>> byCase, int comparedCount)
    {
        var sb = new StringBuilder();
        sb.Append("# Endpoint parity diff - ").Append(group).Append(" (").Append(grant).Append(")\n\n");

        var changed = byCase.Where(kv => kv.Value.Count > 0).OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();

        sb.Append("Cases compared: ").Append(comparedCount)
          .Append(" | cases with differences: ").Append(changed.Count).Append("\n\n");

        if (changed.Count == 0)
        {
            sb.Append("No differences.\n");
            return sb.ToString();
        }

        sb.Append("Every difference below is either a bug just introduced, or an intended change ")
          .Append("that must be recorded in the commit message and accepted explicitly with\n")
          .Append("`parity.ps1 accept`. Re-running `capture` to make a diff go away destroys the baseline.\n\n");

        foreach (var (caseName, diffs) in changed)
        {
            sb.Append("## ").Append(caseName).Append("  (").Append(diffs.Count).Append(" differences)\n\n");
            foreach (var d in diffs.Take(200))
                sb.Append("- ").Append(d).Append('\n');
            if (diffs.Count > 200)
                sb.Append("- ... ").Append(diffs.Count - 200).Append(" more\n");
            sb.Append('\n');
        }

        return sb.ToString();
    }
}
