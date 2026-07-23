using ShiftSoftware.ADP.Darlastic.Engine;

namespace ShiftSoftware.ADP.Darlastic.CaseBrowser;

/// <summary>
/// Cross-machine shared state — the labeled corpus + the review queue — lives on the mounted
/// Azure File Share next to the source data (CompanyInvoiceData), NOT in local out/ and NOT in
/// git: it is real customer PII, and the spike's git remote is cloud GitHub. Each dev machine
/// keeps this one folder in sync by hand (alongside CompanyInvoiceData); in prod it maps to the
/// tenant's own in-boundary Azure File Share. See .shift/guides/local-dev.md.
///
/// Only the human-decision artifacts live here. Everything else under out/ (real_attributes,
/// merge_*, the labeling sample, eval_errors, explainer.html, …) is regenerated from the source
/// data and stays local. Override the location with the CC_SHARED_DIR env var — e.g. a machine
/// that mounts the share somewhere other than C:\mounts\adp-sync-agent-source.
/// </summary>
public static class SharedStore
{
    public static string Dir =>
        Environment.GetEnvironmentVariable("CC_SHARED_DIR") is { Length: > 0 } d
            ? d
            : @"C:\mounts\adp-sync-agent-source\CentralizedCustomerCorpus";

    public static string GoldSet => Path.Combine(Dir, "gold_set.csv");
    public static string LabeledPairs => Path.Combine(Dir, "labeled_pairs.csv");
    public static string LabelAudits => Path.Combine(Dir, "label_audits.csv");
    public static string ReviewNotes => Path.Combine(Dir, "review_notes.jsonl");

    /// <summary>Create the shared folder if absent (it normally already exists — synced alongside
    /// the source data — but a first write on a not-yet-synced machine shouldn't throw).</summary>
    public static void EnsureDir() => Directory.CreateDirectory(Dir);
}
