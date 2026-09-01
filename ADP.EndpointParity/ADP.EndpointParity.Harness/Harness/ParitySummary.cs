using System.Text;

namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER: HttpClient, System.Text.Json and string only.

/// <summary>
/// The gates, computed over a captured run. <b>A near-empty or all-error baseline is the single
/// most common way this whole exercise silently fails</b>, so every number here exists to make
/// one specific silent failure loud.
/// </summary>
public sealed class ParitySummary
{
    public required string Group { get; init; }
    public required string Grant { get; init; }

    public int Cases { get; init; }
    public int Status2xx { get; init; }
    public int Status4xx { get; init; }
    public int Status5xx { get; init; }
    public int EmptyBodies { get; init; }
    public int Partial { get; init; }

    public int CreateTotal { get; init; }
    public int Create2xx { get; init; }
    public int UpdateTotal { get; init; }
    public int Update2xx { get; init; }

    /// <summary>Entities exempted from the write gate, with a written reason, from parity.psd1.</summary>
    public IReadOnlyCollection<string> WriteUnreachable { get; init; } = Array.Empty<string>();

    public int CatalogueRoutes { get; init; }
    public int CatalogueCovered { get; init; }
    public int CatalogueExcluded { get; init; }

    /// <summary>
    /// The gate that replaces "&gt; 0 rows". The sample hosts seed their own demo rows at
    /// startup, so a row-count gate is satisfied by the demo seed ALONE and cannot distinguish
    /// "the adversarial parity seed was applied" from "only the sample's demo data is present".
    /// This counts hostile seed ids found LITERALLY in a list body instead.
    /// </summary>
    public int HostileRowsExpected { get; init; }
    public int HostileRowsPresent { get; init; }

    public IReadOnlyCollection<string> MissingHostileRows { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Routes belonging to other packages (identity dashboard, auth, static fallback). Reported so
    /// the omission is visible, but not counted against this group's coverage - a gate that can
    /// never pass gets "satisfied" by weakening it, which is worse than a gate scoped honestly.
    /// </summary>
    public IReadOnlyCollection<string> OutOfScopeRoutes { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> HardFailures { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SuspectedVolatile { get; init; } = Array.Empty<string>();

    /// <summary>Computes the summary from a completed run.</summary>
    public static ParitySummary From(
        string group,
        ParityGrant grant,
        IReadOnlyList<Transcript> transcripts,
        IReadOnlyCollection<string> hostileIds,
        IReadOnlyCollection<string> catalogueRoutes,
        IReadOnlyCollection<string> exercisedRoutes,
        IReadOnlyCollection<string> excludedRoutes,
        IReadOnlyCollection<string> writeUnreachable,
        IReadOnlyCollection<string> hardFailures,
        IReadOnlyCollection<string>? outOfScopeRoutes = null)
    {
        // Hostile-row presence is checked against LIST bodies only, and by literal substring:
        // the id must appear as the host actually rendered it. Rule 1 makes that legitimate -
        // explicit long PKs plus a pinned hash-id salt mean a seeded id is the same text on
        // every run, so a literal match is exact rather than approximate.
        var listBodies = transcripts
            .Where(t => t.Kind == "LIST" && t.Body is not null)
            .Select(t => t.Body!)
            .ToList();

        var missing = hostileIds
            .Where(id => !listBodies.Any(b => b.Contains(id, StringComparison.Ordinal)))
            .ToList();

        return new ParitySummary
        {
            Group = group,
            Grant = grant.ToString(),
            Cases = transcripts.Count,
            Status2xx = transcripts.Count(t => t.Status is >= 200 and < 300),
            Status4xx = transcripts.Count(t => t.Status is >= 400 and < 500),
            Status5xx = transcripts.Count(t => t.Status >= 500),
            EmptyBodies = transcripts.Count(t => t.Partial is null && string.IsNullOrWhiteSpace(t.Body)),
            Partial = transcripts.Count(t => t.Partial is not null),
            CreateTotal = transcripts.Count(t => t.Kind == "CREATE"),
            Create2xx = transcripts.Count(t => t.Kind == "CREATE" && t.Status is >= 200 and < 300),
            UpdateTotal = transcripts.Count(t => t.Kind == "UPDATE"),
            Update2xx = transcripts.Count(t => t.Kind == "UPDATE" && t.Status is >= 200 and < 300),
            WriteUnreachable = writeUnreachable,
            CatalogueRoutes = catalogueRoutes.Count,
            CatalogueCovered = catalogueRoutes.Count(r => exercisedRoutes.Contains(r) || excludedRoutes.Contains(r)),
            CatalogueExcluded = excludedRoutes.Count,
            HostileRowsExpected = hostileIds.Count,
            HostileRowsPresent = hostileIds.Count - missing.Count,
            MissingHostileRows = missing,
            HardFailures = hardFailures,
            OutOfScopeRoutes = outOfScopeRoutes ?? Array.Empty<string>(),
            SuspectedVolatile = transcripts.SelectMany(t => t.SuspectedVolatile).Distinct(StringComparer.Ordinal).ToList(),
        };
    }

    /// <summary>
    /// True when every gate in verification.md section 5 / section 8 passes. The driver's exit
    /// code is this.
    /// </summary>
    /// <summary>True when the run was made under the restricted principal.</summary>
    public bool IsRestricted => Grant == nameof(ParityGrant.Restricted);

    public bool Passes =>
        Status5xx == 0 &&
        HardFailures.Count == 0 &&
        // FULL ACCESS: every hostile row must be visible. That is the gate proving the
        // adversarial seed actually reached the database, and it replaces "> 0 rows".
        //
        // RESTRICTED: a hostile row on an entity this principal cannot read is EXPECTED to be
        // absent - which is the whole point of the pass. Requiring all of them would make the
        // restricted baseline uncapturable. At least one must still be visible, so a restricted
        // run against an empty database is still caught rather than passing vacuously.
        //
        // ...UNLESS the group has no hostile rows to begin with. A group with 0 triples has no
        // adversarial seed by construction (there is nothing mapper-shaped to trap), and
        // "0 > 0" would fail it forever for a reason that has nothing to do with its behaviour.
        // Found on Darlastic, which is exactly that group.
        (IsRestricted
            ? (HostileRowsExpected == 0 || HostileRowsPresent > 0)
            : MissingHostileRows.Count == 0) &&
        CatalogueCovered == CatalogueRoutes &&
        // The 100%-write gates apply to the FULL-ACCESS pass only. Under a read-only grant a
        // refused CREATE is the correct answer, and demanding 2xx there would make the gate
        // unsatisfiable - which is how gates get quietly weakened.
        (IsRestricted || CreateTotal == 0 || Create2xx == CreateTotal) &&
        (IsRestricted || UpdateTotal == 0 || Update2xx == UpdateTotal);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("cases: ").Append(Cases)
          .Append(" | 2xx: ").Append(Status2xx)
          .Append(" | 4xx: ").Append(Status4xx)
          .Append(" | 5xx: ").Append(Status5xx)
          .Append(" | empty bodies: ").Append(EmptyBodies)
          .Append(" | PARTIAL: ").Append(Partial).Append('\n');

        sb.Append("CREATE 2xx: ").Append(Create2xx).Append('/').Append(CreateTotal)
          .Append(" | UPDATE 2xx: ").Append(Update2xx).Append('/').Append(UpdateTotal)
          .Append(" | catalogue routes covered: ").Append(CatalogueCovered).Append('/').Append(CatalogueRoutes)
          .Append(", excluded: ").Append(CatalogueExcluded).Append('\n');

        sb.Append("hostile seed rows present in list bodies: ")
          .Append(HostileRowsPresent).Append('/').Append(HostileRowsExpected).Append('\n');

        if (WriteUnreachable.Count > 0)
            sb.Append("writeUnreachable (mapper-level golden required instead): ")
              .Append(string.Join(", ", WriteUnreachable)).Append('\n');

        if (MissingHostileRows.Count > 0 && IsRestricted)
        {
            sb.Append("\nNOTE - hostile seed rows not visible under the restricted grant:\n");
            foreach (var m in MissingHostileRows) sb.Append("  - ").Append(m).Append('\n');
            sb.Append("  Expected wherever this principal lacks read access to that entity - recording\n")
              .Append("  which rows a restricted caller CANNOT see is the purpose of this pass. Each one\n")
              .Append("  must still appear in the FULL-ACCESS baseline, where the gate is strict.\n");
        }
        else if (MissingHostileRows.Count > 0)
        {
            sb.Append("\nFAIL - hostile seed rows NOT found in any list body:\n");
            foreach (var m in MissingHostileRows) sb.Append("  - ").Append(m).Append('\n');
            sb.Append("  The adversarial seed did not reach the database, or the list case does not ")
              .Append("cover it.\n  A baseline without these rows cannot fail, and proves nothing.\n");
        }

        if (CatalogueCovered != CatalogueRoutes)
            sb.Append("\nFAIL - ").Append(CatalogueRoutes - CatalogueCovered)
              .Append(" catalogue route(s) neither covered by a case nor listed in excludedRoutes with a reason.\n");

        if (IsRestricted)
            sb.Append("restricted pass: write gates not applied (a refused CREATE is the expected answer)\n");

        if (!IsRestricted && CreateTotal > 0 && Create2xx != CreateTotal)
            sb.Append("\nFAIL - CREATE is not 100% 2xx. A body that 4xxs never reaches the mapper, so the ")
              .Append("write path\n  covers nothing while every other gate stays green. Fix the minimal-valid body, ")
              .Append("or list the\n  entity in writeUnreachable with a written reason.\n");

        if (!IsRestricted && UpdateTotal > 0 && Update2xx != UpdateTotal)
            sb.Append("\nFAIL - UPDATE is not 100% 2xx (same reasoning as CREATE).\n");

        if (Status5xx > 0)
            sb.Append("\nFAIL - ").Append(Status5xx).Append(" case(s) returned 5xx.\n");

        if (HardFailures.Count > 0)
        {
            sb.Append("\nFAIL - hard failures:\n");
            foreach (var f in HardFailures) sb.Append("  - ").Append(f).Append('\n');
        }

        if (SuspectedVolatile.Count > 0)
        {
            sb.Append("\nNOTE - values that look like a timestamp inside the run window but are NOT on the\n")
              .Append("  Rule 2 name allowlist. These are REPORTED, never normalized. Classify each once,\n")
              .Append("  then add it to the allowlist deliberately if it really is volatile:\n");
            foreach (var v in SuspectedVolatile.Take(40)) sb.Append("  - ").Append(v).Append('\n');
            if (SuspectedVolatile.Count > 40)
                sb.Append("  - ... ").Append(SuspectedVolatile.Count - 40).Append(" more\n");
        }

        sb.Append('\n').Append(Passes ? "GATES: PASS" : "GATES: FAIL").Append('\n');
        return sb.ToString();
    }
}
