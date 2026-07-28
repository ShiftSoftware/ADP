using System.Collections.Generic;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.Steward;

/// <summary>
/// One case on the steward's work list: a candidate pair the engine scored inside the steward band
/// [0.80, auto-merge) and refused to decide, with both source records' normalized evidence attached.
///
/// <para>Read from the registry's own <c>StewardQueue</c> / <c>StewardRecord</c> tables, which the
/// resolve run stages. That staging is what makes this surface cheap: the queue is served by key
/// seeks over ~10⁴ rows instead of re-scoring the corpus in memory, which is the minutes-and-
/// gigabytes shape the case-browser prototype had.</para>
///
/// <para>Carries evidence, not reasoning. The score is staged; the matcher's trace (which rule
/// fired, how the name blend resolved) is computed during a resolve and never persisted, so the
/// steward sees WHAT the two records say and the engine's confidence — not the derivation. Adding
/// the derivation needs a new staged artifact family from the engine, not a richer query here.</para>
/// </summary>
public class StewardCaseDTO
{
    /// <summary>Canonical "src:id~src:id" — the identifier for this pair everywhere in the system,
    /// and the key a verdict is recorded against.</summary>
    public string PairKey { get; set; } = default!;

    /// <summary>The engine's confidence, staged by the resolve that queued this pair.</summary>
    public float Score { get; set; }

    /// <summary>The resolve run that queued it. The queue is replaced wholesale every run, so this
    /// is always the latest run — surfaced so a steward can tell how fresh the work list is.</summary>
    public int RunID { get; set; }

    public StewardCaseRecordDTO? A { get; set; }
    public StewardCaseRecordDTO? B { get; set; }

    /// <summary>
    /// The verdict already standing on this pair, if any (<c>merge</c> / <c>separate</c> /
    /// <c>defer</c>). Normally null: a decided pair is suppressed from the next run's queue, so a
    /// non-null value here means the verdict was recorded after the last resolve and has not been
    /// replayed yet — worth showing, because the golden list will not reflect it until then.
    /// </summary>
    public string? StandingVerdict { get; set; }
}

/// <summary>
/// One side of a case — the normalized source record as the matcher saw it, deserialized from the
/// JSON the resolve staged. Normalized rather than raw on purpose: the steward should judge the
/// same text the engine judged.
/// </summary>
public class StewardCaseRecordDTO
{
    public string SourceSystem { get; set; } = default!;
    public string SourceRecordId { get; set; } = default!;

    /// <summary>As the source spells it — what a steward recognizes.</summary>
    public string? RawName { get; set; }

    /// <summary>As the matcher reads it (transliterated, mojibake-repaired, lowercased).</summary>
    public string? NormName { get; set; }

    public List<string> Phones { get; set; } = new();
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? NationalId { get; set; }

    /// <summary>The identity this record currently belongs to, if the registry has assigned one.</summary>
    public string? IdentityID { get; set; }
}

/// <summary>One page of the steward's work list.</summary>
public class StewardQueuePageDTO
{
    /// <summary>Open cases in the whole queue — the work-list depth Phase 6 watches as a health metric.</summary>
    public int Total { get; set; }

    public int Skip { get; set; }

    public List<StewardCaseDTO> Cases { get; set; } = new();
}

/// <summary>A steward's verdict on one case.</summary>
public class StewardVerdictDTO
{
    public string PairKey { get; set; } = default!;

    /// <summary>One of <c>ShiftSoftware.ADP.Darlastic.Engine.StewardVerdict</c>'s constants —
    /// merge / separate / defer / release. Validated server-side.</summary>
    public string Verdict { get; set; } = default!;

    /// <summary>Optional free text — why. Audited with the verdict.</summary>
    public string? Note { get; set; }
}

/// <summary>What a recorded verdict did, so the caller can reflect it without a re-fetch.</summary>
public class StewardVerdictResultDTO
{
    public string PairKey { get; set; } = default!;
    public string Verdict { get; set; } = default!;

    /// <summary>Prior standing decisions this verdict deactivated.</summary>
    public int SupersededDecisions { get; set; }

    /// <summary>
    /// True when the verdict constrains the engine (merge / separate). False for defer and release,
    /// which are audited but change no clustering.
    /// </summary>
    public bool IsConstraint { get; set; }
}
