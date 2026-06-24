namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Presentation customization for <see cref="DashboardRenderer"/>. The framework renders generically by
/// default (the structural Freshness / Reconciliation / Quality / Volume / Flow vocabulary, underscores →
/// spaces, title-case); a consumer supplies these to map its own check names, acronyms, and breakdown keys
/// to friendly labels — keeping domain knowledge in the domain pack instead of the framework.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Friendly-name overrides for name tokens (check/family tokens, breakdown keys before formatting),
    /// merged over — and winning against — the framework's structural defaults. Keys are matched
    /// case-insensitively, e.g. <c>["orders_daily"] = "Daily orders"</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string>? TokenLabels { get; init; }

    /// <summary>Tokens rendered upper-cased rather than title-cased (domain acronyms, e.g. an "id" token → "ID"). Empty by default.</summary>
    public IReadOnlySet<string>? Acronyms { get; init; }

    /// <summary>
    /// Maps a raw <c>breakdownKey</c> to its display label (e.g. strip a source file-name prefix so a
    /// grouped row reads "north" instead of "feed_north"). Identity by default.
    /// </summary>
    public Func<string, string>? BreakdownKeyFormatter { get; init; }

    /// <summary>
    /// Category groups in display order, e.g. <c>["freshness","reconciliation","quality","volume","flow"]</c>.
    /// Listed categories render in this order; any not listed fall after, in the framework's built-in order then
    /// alphabetically. Null keeps the built-in order. Matched case-insensitively on the category token.
    /// </summary>
    public IReadOnlyList<string>? CategoryOrder { get; init; }

    /// <summary>Generic defaults — no domain-specific labels.</summary>
    public static readonly DashboardOptions Default = new();
}
