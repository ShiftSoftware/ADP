using System.Net.Http.Headers;
using System.Text;
using ShiftSoftware.ADP.EndpointParity.Harness;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>Everything a group project must supply to run a parity pass.</summary>
public sealed class ParityGroupConfig
{
    public required string Group { get; init; }
    public required string RoutePrefix { get; init; }

    /// <summary>Every TypeAuth tree the host registers, granted [1,2,3,4] under FullAccess.</summary>
    public required IReadOnlyCollection<string> ActionTrees { get; init; }

    /// <summary>What "Restricted" means HERE. Each group has its own tree, so this cannot be shared.</summary>
    public required IReadOnlyDictionary<string, int[]> RestrictedGrant { get; init; }

    public required string Issuer { get; init; }
    public required string PrivateKeyBase64 { get; init; }

    /// <summary>Catalogue routes deliberately not covered, each with a reason in parity.psd1.</summary>
    public IReadOnlyCollection<string> ExcludedRoutes { get; init; } = Array.Empty<string>();

    /// <summary>Entities exempt from the 100% write gate; each needs a mapper-level golden instead.</summary>
    public IReadOnlyCollection<string> WriteUnreachable { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> CreateBodies { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> UpdateBodies { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Hash ids of seeded rows per entity route segment, for DETAIL/REVISIONS per row.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SeededHashIds { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Markers of hostile rows; the gate checks each appears literally in a list body.</summary>
    public IReadOnlyCollection<string> HostileMarkers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// False where the group's tables are not system-versioned, so the inherited asOf route would
    /// 500. See CaseListBuilder's constructor for why this is a real condition and not paranoia.
    /// </summary>
    public bool EmitAsOfCases { get; init; } = true;

    /// <summary>$top for list cases under the RESTRICTED grant, where the page-size cap is lower.</summary>
    public int RestrictedListTop { get; init; } = 5;

    /// <summary>
    /// $top under FULL ACCESS. Not a constant either: a MOUNTED host has no identity server, so it
    /// runs anonymous and is subject to the same low page-size cap as a restricted principal
    /// ("The requested number of records (25) exceeds the maximum allowed limit of 5"). Mounted
    /// groups set this to 5; sample-host groups keep 25.
    /// </summary>
    public int FullAccessListTop { get; init; } = 25;

    public NormalizerOptions Normalization { get; init; } = new();
}

/// <summary>
/// One parity pass for one group under one grant: enumerate, build cases, run, write or diff, gate.
///
/// <para>
/// Lives here rather than in each group project so the five group projects stay THIN — they supply
/// a host and a config and nothing else. The mode comes from <c>PARITY_MODE</c>, which
/// <c>tools/parity.ps1</c> sets.
/// </para>
/// </summary>
public static class ParityGroupRun
{
    public static async Task<ParitySummary> ExecuteAsync(
        HttpClient client,
        IServiceProvider hostServices,
        ParityGroupConfig config,
        ParityGrant grant,
        ParityMode mode,
        string parityRoot,
        CancellationToken ct = default)
    {
        // ---- token for this grant ------------------------------------------------------
        // A MOUNTED host has no identity server and validates no bearer token, so it supplies no
        // signing key and no token is minted. Say that plainly rather than minting an unsigned
        // stand-in: it means the RESTRICTED pass for a mounted group exercises no privilege
        // boundary at all, which is a real limitation of that host mode and is recorded as one.
        if (!string.IsNullOrWhiteSpace(config.PrivateKeyBase64))
        {
            var accessTree = ParityAuth.BuildAccessTree(grant, config.ActionTrees, config.RestrictedGrant);
            var token = ParityAuth.MintToken(config.Issuer, config.PrivateKeyBase64, accessTree);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // ---- route catalogue: a golden in its own right, AND the source of the case list ----
        var routes = RouteCatalog.Enumerate(hostServices);
        var catalogueDir = Path.Combine(parityRoot, "baselines", config.Group.ToLowerInvariant());
        Directory.CreateDirectory(catalogueDir);
        var cataloguePath = Path.Combine(catalogueDir, "route-catalogue.json");
        var catalogueGolden = RouteCatalog.ToGolden(routes);

        if (mode == ParityMode.Capture)
        {
            File.WriteAllText(cataloguePath, catalogueGolden, new UTF8Encoding(false));
        }
        else if (File.Exists(cataloguePath))
        {
            // A route that DISAPPEARED in the upgrade is caught here and nowhere else - a
            // URL-driven harness would simply stop asking for it and report green.
            var diffs = TranscriptDiffer.Diff(File.ReadAllText(cataloguePath), catalogueGolden);
            if (diffs.Count > 0)
            {
                var reportDir = Path.Combine(parityRoot, "reports", config.Group.ToLowerInvariant());
                Directory.CreateDirectory(reportDir);
                File.WriteAllText(Path.Combine(reportDir, "route-catalogue.diff.md"),
                    TranscriptDiffer.Report(config.Group, grant.ToString(),
                        new Dictionary<string, IReadOnlyList<TranscriptDifference>> { ["route-catalogue"] = diffs }));
            }
        }

        // ---- case list, driven FROM the catalogue --------------------------------------
        var builder = new CaseListBuilder(config.RoutePrefix, config.ExcludedRoutes, config.EmitAsOfCases,
            listTop: grant == ParityGrant.Restricted ? config.RestrictedListTop : config.FullAccessListTop);
        var cases = builder.Build(routes, config.SeededHashIds, config.CreateBodies, config.UpdateBodies);

        // ---- run ------------------------------------------------------------------------
        var baselineDir = Path.Combine(catalogueDir, grant.ToString());

        // A capture REPLACES the baseline; it does not merge into it. Without this, goldens from
        // a previous case list survive - observed when disabling the asOf cases left their 500
        // transcripts on disk, where a later verify would have compared against cases the harness
        // no longer issues. A stale golden is indistinguishable from a real one in a diff.
        // Files, not the directory: removing the directory itself fails whenever anything holds a
        // handle on it (a shell sitting in it, an editor), and that failure would abort a capture
        // for a reason unrelated to the run.
        if (mode == ParityMode.Capture && Directory.Exists(baselineDir))
            foreach (var stale in Directory.GetFiles(baselineDir, "*.json"))
                File.Delete(stale);
        var normalizer = new Normalizer(config.Normalization);
        var runner = new ParityRunner(client, normalizer, baselineDir, mode, grant);

        await runner.RunAsync(cases, ct);

        // ---- gates ----------------------------------------------------------------------
        var hardFailures = new List<string>(runner.HardFailures);
        foreach (var uncovered in builder.Uncovered)
            hardFailures.Add("catalogue route not covered and not excluded: " + uncovered);

        // Coverage is measured over THIS GROUP's routes only. Counting the ShiftIdentity dashboard
        // and auth surface against ADP.Surveys would make the gate unsatisfiable and would then be
        // "satisfied" by weakening it - the exact failure the plan warns about with unsatisfiable
        // criteria. Out-of-scope routes are reported separately instead.
        var inScopeRoutes = routes.Select(r => r.Key).Distinct()
            .Where(k => !builder.OutOfScope.Contains(k)).ToList();

        var summary = ParitySummary.From(
            config.Group,
            grant,
            runner.Transcripts,
            config.HostileMarkers,
            inScopeRoutes,
            runner.ExercisedRoutes,
            config.ExcludedRoutes,
            config.WriteUnreachable,
            hardFailures,
            builder.OutOfScope);

        if (mode == ParityMode.Verify)
        {
            var reportDir = Path.Combine(parityRoot, "reports", config.Group.ToLowerInvariant());
            Directory.CreateDirectory(reportDir);
            var reportPath = Path.Combine(reportDir, "diff.md");

            if (runner.Differences.Count > 0)
            {
                File.WriteAllText(reportPath,
                    TranscriptDiffer.Report(config.Group, grant.ToString(),
                        runner.Differences.ToDictionary(kv => kv.Key, kv => kv.Value)));
            }
            else if (File.Exists(reportPath))
            {
                // A CLEAN verify must REMOVE the previous report, not leave it lying there.
                // Observed at Step 01: a clean run left the prior run's diff.md on disk, so the
                // directory listing still advertised differences that no longer existed. Same
                // failure shape as a stale golden - an artifact outliving the run that produced
                // it, and read as if it described the current one.
                File.Delete(reportPath);
            }
        }

        return summary;
    }

    /// <summary>Reads the mode tools/parity.ps1 asked for. Defaults to verify - the SAFE verb.</summary>
    public static ParityMode ModeFromEnvironment() =>
        Environment.GetEnvironmentVariable("PARITY_MODE")?.ToLowerInvariant() == "capture"
            ? ParityMode.Capture
            : ParityMode.Verify;

    public static ParityGrant GrantFromEnvironment() =>
        Environment.GetEnvironmentVariable("PARITY_GRANT")?.Equals("Restricted", StringComparison.OrdinalIgnoreCase) == true
            ? ParityGrant.Restricted
            : ParityGrant.FullAccess;

    public static string ParityRootFromEnvironment(string fallback) =>
        Environment.GetEnvironmentVariable("PARITY_ROOT") ?? fallback;
}
