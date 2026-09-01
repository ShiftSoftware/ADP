namespace ShiftSoftware.ADP.EndpointParity.Harness;

// CAPTURE LAYER: HttpClient, System.Text.Json and string only.

/// <summary>
/// Per-group normalization configuration, loaded from tools/parity.psd1 by the driver and
/// handed to the runner. Every setting here WIDENS normalization, so every setting here is
/// a deliberate, reviewed act (verification.md Rule 4) — the defaults are the strict case.
/// </summary>
public sealed class NormalizerOptions
{
    /// <summary>
    /// Response headers to keep, beyond status and Content-Type. Rule 3 drops everything
    /// else — Date, Server, Set-Cookie, Content-Length, Request-Context, traceparent — because
    /// they are volatile by construction and would swamp the diff.
    /// </summary>
    public HashSet<string> HeaderAllowlist { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// JSON paths whose array order is genuinely irrelevant, in pattern form with `[]` for
    /// any index — e.g. `$.response.body.value[].Tags`. Rule 4: child collections preserve
    /// source order BY DEFAULT because sort order is semantic in several places, and sorting
    /// a collection to "stabilize" it erases an ordering regression. An entry here is a
    /// deliberate act reviewed in the PR, not a default.
    /// </summary>
    public HashSet<string> OrderInsensitivePaths { get; init; } =
        new(StringComparer.Ordinal);

    /// <summary>
    /// When the run started. Rule 2's safety net flags any value parsing as a timestamp inside
    /// [RunStart - 5min, now] that is NOT on the name allowlist — reported, never normalized.
    /// </summary>
    public DateTimeOffset RunStart { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Rule 1: OFF by default and it must stay off unless run-to-run drift is actually
    /// OBSERVED, and only after trying to make the value deterministic instead. Created IDs
    /// compare LITERALLY by default. Turning this on trades away trap-2 coverage on the write
    /// path, so the alias is keyed by JSON path of first occurrence — never a bare counter,
    /// which would render a parent's id and a child slot carrying that same id as the same
    /// token and fire no diff at all.
    /// </summary>
    public bool EnableCreatedIdAliasing { get; init; }

    /// <summary>
    /// Rule 2's name allowlist. These, and only these, become &lt;ts&gt;. ValidFrom/ValidTo are
    /// handled separately: they normalize ONLY inside a Revisions array.
    /// </summary>
    public HashSet<string> TimestampNames { get; init; } =
        new(StringComparer.Ordinal) { "CreateDate", "LastSaveDate" };

    /// <summary>
    /// Members the SERVER generates with <c>Guid.NewGuid()</c> on create. Observed drift, not
    /// anticipated drift: two identical capture runs produced different BankEntryID values, and
    /// the value is minted inside the repository with no seam to pin it - so unlike a timestamp
    /// salt or a seeded long, it genuinely cannot be made deterministic without changing
    /// production source.
    ///
    /// <para>
    /// <b>The signal is preserved where it matters.</b> A value listed in
    /// <see cref="KnownDeterministicValues"/> - i.e. one the SEED wrote - is still compared
    /// literally; only values the server invented during this run are replaced. So a wrong
    /// BankEntryID on a seeded row is still a diff, and only the unavoidable freshly-minted one
    /// is tokenised. GIVES UP: detection of a regression that changes which NEW guid a created
    /// row receives - which is not a distinction any consumer can observe.
    /// </para>
    /// </summary>
    public HashSet<string> ServerGeneratedGuidNames { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Values written by the seed. These stay literal even under the rule above.</summary>
    public HashSet<string> KnownDeterministicValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
