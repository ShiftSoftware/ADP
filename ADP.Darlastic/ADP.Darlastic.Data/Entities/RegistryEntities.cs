using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Data.Entities;

/// <summary>
/// A golden identity — the stable customer ID consumers reference. Append-only; never reused.
/// Written by the engine's reconciliation (set-based), read by the API/steward surfaces.
/// </summary>
public class GoldenIdentity
{
    public long IdentityID { get; set; }
    public IdentityStatus Status { get; set; }
    public int CreatedRunID { get; set; }
    public int LastChangedRunID { get; set; }
}

/// <summary>Merge redirect: consumers holding <see cref="OldIdentityID"/> resolve one hop to the
/// surviving identity (chains are compressed on write, so one hop always lands live).</summary>
public class IdentityRedirect
{
    public long OldIdentityID { get; set; }
    public long NewIdentityID { get; set; }
    public int RunID { get; set; }
}

/// <summary>
/// One row per (source system, source record id) — the engine's assignment ledger. Soft-removed
/// (never deleted) so a record that reappears after a transient source glitch revives its old
/// identity instead of minting a new one.
/// </summary>
public class SourceProfile
{
    public string SourceSystem { get; set; } = default!;
    public string SourceRecordId { get; set; } = default!;
    public long IdentityID { get; set; }
    public string ContentHash { get; set; } = default!;
    public bool Removed { get; set; }
    public int? RemovedRunID { get; set; }
    public int FirstRunID { get; set; }
    public int LastChangedRunID { get; set; }
}

/// <summary>
/// Delta-out staging: the content hash of every projected artifact (golden docs, ownership links).
/// The engine marks Pending only on hash change; the projection drain writes Pending artifacts to
/// Cosmos and stamps <see cref="ProjectedRunID"/>. Hash 'TOMBSTONE' means delete downstream.
/// </summary>
public class ProjectionState
{
    public string ArtifactType { get; set; } = default!;
    public string ArtifactKey { get; set; } = default!;
    public string ContentHash { get; set; } = default!;
    public bool Pending { get; set; }
    public int UpdatedRunID { get; set; }
    public int? ProjectedRunID { get; set; }
    public string? Payload { get; set; }
}

/// <summary>Immutable audit log — every steward action (verdict, flag, merge, split, override),
/// who/when/what, with the action payload as JSON. Append-only by contract.</summary>
public class AuditEntry
{
    public long AuditID { get; set; }
    public DateTime AtUtc { get; set; }
    public string Actor { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string TargetKey { get; set; } = default!;
    public string? Payload { get; set; }
}

/// <summary>An engine-honored steward constraint (P4 mode 3): merge / split / sticky attribute
/// override / defer. The engine replays Active rows as hard constraints on every resolve.</summary>
public class StewardDecision
{
    public long DecisionID { get; set; }
    public DateTime AtUtc { get; set; }
    public string Actor { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public long? IdentityID { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }
    public string? AttrType { get; set; }
    public string? Value { get; set; }
    public bool Active { get; set; }
    public string? Payload { get; set; }
}

/// <summary>
/// A candidate pair the engine could not decide — the steward's work list. Rewritten wholesale on
/// every resolve (it is derived state, not a ledger), so it carries no history of its own; a
/// steward's verdict lands in <see cref="AuditEntry"/> and, when it constrains the engine, in
/// <see cref="StewardDecision"/>.
/// </summary>
public class StewardQueueEntry
{
    /// <summary>"src:id~src:id" in canonical order — the same key the case browser audits against.</summary>
    public string PairKey { get; set; } = default!;
    public int RunID { get; set; }
    public float Score { get; set; }
    public string SourceSystemA { get; set; } = default!;
    public string SourceRecordIdA { get; set; } = default!;
    public string SourceSystemB { get; set; } = default!;
    public string SourceRecordIdB { get; set; } = default!;
}

/// <summary>
/// The normalized record behind a queued or catalogued pair, stored as JSON so a browsing surface
/// can render the evidence without re-reading (or re-normalizing) the source systems. Rewritten
/// with the queue and catalog.
/// </summary>
public class StewardRecord
{
    public string SourceSystem { get; set; } = default!;
    public string SourceRecordId { get; set; } = default!;
    public int RunID { get; set; }
    public string Payload { get; set; } = default!;
}

/// <summary>
/// One auto-merge edge: the evidence that joined two records into an identity. Written by the
/// resolve's own pair walk, which is the only pass that sees them — the registry otherwise records
/// that a record BELONGS to an identity (<see cref="SourceProfile"/>) but never WHY, leaving
/// "how was this identity assembled" unanswerable by any query.
///
/// <para>Derived state, rewritten wholesale each resolve. Corroborating edges (both records already
/// joined via a third) are kept deliberately: how strongly a cluster is held together is evidence a
/// steward reads before splitting it.</para>
/// </summary>
public class IdentityEdge
{
    public long EdgeID { get; set; }
    public int RunID { get; set; }

    /// <summary>The identity both records belong to after this run — the grouping key for
    /// "show me how identity N was assembled".</summary>
    public long IdentityID { get; set; }

    public string SourceSystemA { get; set; } = default!;
    public string SourceRecordIdA { get; set; } = default!;
    public string SourceSystemB { get; set; } = default!;
    public string SourceRecordIdB { get; set; } = default!;

    public float Score { get; set; }

    /// <summary>The engine's <c>MatchFlags</c> bitmask — which rules fired on this edge. Stored raw
    /// so a surface can name the rules without the engine re-scoring anything.</summary>
    public long Flags { get; set; }
}

/// <summary>
/// A capped, browsable sample of decision-relevant pairs per category. The companion to
/// <see cref="CaseCategoryCount"/>: counts say how big a category is, these let a human look inside
/// it. Rewritten wholesale each resolve.
///
/// <para>Sampled first-encountered in the resolve's deterministic block order — reproducible
/// run-to-run (the zero-delta acceptance depends on that) but biased by blocking, so it is a
/// browsing aid and never a basis for a statistic.</para>
/// </summary>
public class CaseCatalogEntry
{
    /// <summary>"src:id~src:id" in canonical order — the same pair identity used everywhere.</summary>
    public string PairKey { get; set; } = default!;
    public int RunID { get; set; }
    public float Score { get; set; }

    /// <summary>The engine's <c>CaseCat</c> bitmask — every category this pair carries.</summary>
    public long Categories { get; set; }

    /// <summary>The engine's <c>MatchFlags</c> bitmask — which rules fired.</summary>
    public long Flags { get; set; }

    public string SourceSystemA { get; set; } = default!;
    public string SourceRecordIdA { get; set; } = default!;
    public string SourceSystemB { get; set; } = default!;
    public string SourceRecordIdB { get; set; } = default!;
}

/// <summary>
/// Per-identity browsing summary: size, spread, survived name, and the cluster-level categories.
///
/// <para>Staged because none of it is recoverable by query. <c>SourceProfile</c> can be grouped to
/// count members, but that is a scan of the whole corpus per page; and the interesting categories
/// are not countable at all — "the golden name was extended to a fuller chain than the most-attested
/// spelling" and "a shared sold VIN merged records that name and phone alone could not" are facts
/// known only while survivorship and scoring are running.</para>
///
/// <para>Multi-record identities only: a single-record identity has nothing to explain, and
/// including them would put a row here for two thirds of the corpus to say so.</para>
///
/// <para>Derived state, rewritten wholesale each resolve.</para>
/// </summary>
public class IdentitySummary
{
    public long IdentityID { get; set; }
    public int RunID { get; set; }

    public int MemberCount { get; set; }

    /// <summary>Distinct source systems contributing — >1 is the initiative's business case.</summary>
    public int SourceCount { get; set; }

    /// <summary>The survived full name, as the golden carries it.</summary>
    public string? GoldenName { get; set; }

    /// <summary>The engine's <c>ClusterCat</c> bitmask.</summary>
    public long Categories { get; set; }
}

/// <summary>
/// A case flagged for discussion, with a free-text note — distinct from a steward VERDICT. The
/// reviewer marks something worth a second opinion and carries the engine's evidence with it;
/// someone (today: Claude, in a later session) answers, and the flag closes.
///
/// <para>Replaces the case browser's <c>review_notes.jsonl</c>. That file was correct for one
/// person on a laptop and lossy for a team: the whole file is rewritten on every save under an
/// in-process lock, so two people clicking at once — the normal case once this is shared — silently
/// drop one of the two edits, and an app restart mid-write truncates it.</para>
///
/// <para>One open flag per target (a pair key or an identity id); re-flagging edits in place. The
/// snapshot is the full case detail INCLUDING the engine trace as it read when flagged, which is
/// what lets the resolution show a before/after once the engine has moved on.</para>
/// </summary>
public class ReviewFlag
{
    public long FlagID { get; set; }

    /// <summary>What was flagged — a pair key ("src:id~src:id") or "identity:{id}".</summary>
    public string Target { get; set; } = default!;

    /// <summary>What kind of feedback this carries — steers the reviewer.</summary>
    public string Topic { get; set; } = default!;

    public string Comment { get; set; } = default!;
    public string Author { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }

    /// <summary>Full case/cluster detail incl. the engine trace at flag time, JSON. Self-contained
    /// on purpose: a reviewer reads the evidence without the engine, the sources, or this app.</summary>
    public string Snapshot { get; set; } = default!;

    /// <summary>Null while open. Set when answered — the flag keeps its history either way.</summary>
    public string? Response { get; set; }
    public string? ResponseBy { get; set; }
    public DateTime? ResponseUtc { get; set; }
}

/// <summary>
/// A human's adjudication of a pair's label — the corpus-growth row. Every steward verdict and
/// every case-browser audit is a candidate labeled pair, and this is where they land before being
/// folded into the gold set.
///
/// <para>Replaces the case browser's <c>label_audits.csv</c>, for the same concurrency reason as
/// <see cref="ReviewFlag"/>. <c>NewLabel</c> empty means "pending an expert call" and is never
/// applied to the gold set — the file format carried that convention and it survives here.</para>
/// </summary>
public class LabelAudit
{
    public long AuditID { get; set; }

    /// <summary>Canonical pair key, or the gold-set numeric id for rows that predate keying.</summary>
    public string PairKey { get; set; } = default!;

    public string? OldLabel { get; set; }

    /// <summary>same / different / ambiguous — or empty for "pending expert call".</summary>
    public string? NewLabel { get; set; }

    public string AuditedBy { get; set; } = default!;
    public DateTime AuditedUtc { get; set; }

    /// <summary>The LLM panel's votes when the pair carried them, as recorded — provenance for how
    /// the original label was reached.</summary>
    public string? PanelVotes { get; set; }

    public string? Rationale { get; set; }

    /// <summary>pending / applied — whether this has been folded into the gold set.</summary>
    public string Status { get; set; } = default!;
}

/// <summary>
/// Exact corpus-wide count per category, from the resolve's full pair walk. Separate from
/// <see cref="CaseCatalogEntry"/> because the sample is capped and the count must never be:
/// a surface that derives category sizes from whatever rows it happens to hold under-reports them
/// by orders of magnitude (measured 2026-07-29 — 13,591 browsable auto-merges against 437,238 that
/// actually fired) while looking entirely plausible.
/// </summary>
public class CaseCategoryCount
{
    /// <summary>The <c>CaseCat</c> member name — stable, readable, and what a sidebar keys on.</summary>
    public string Category { get; set; } = default!;
    public int RunID { get; set; }

    /// <summary>How many pairs in the whole corpus carry this category.</summary>
    public long Total { get; set; }

    /// <summary>How many of them are in <see cref="CaseCatalogEntry"/> — so a surface can say
    /// "showing 500 of 437,238" rather than implying it holds everything.</summary>
    public int Sampled { get; set; }
}

/// <summary>
/// The tenant this registry belongs to — one row, stamped on first resolve and asserted on every
/// open thereafter. The guard exists because the only thing pairing a feed set with a registry is
/// configuration: without it, a misconfigured run would read every foreign source as ABSENT and
/// freeze an entire tenant's corpus. Modeled here so a host's migration creates it; the engine
/// also creates it lazily for the local dev loop.
/// </summary>
public class TenantMarker
{
    public string Tenant { get; set; } = default!;
}

/// <summary>One row per batch resolve run — metrics are the delta discipline's health telemetry
/// (a large write delta on unchanged sources means nondeterminism crept into the engine).</summary>
public class ResolveRun
{
    public int RunID { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime? FinishedUtc { get; set; }
    public int? Records { get; set; }
    public int? Identities { get; set; }
    public int? Minted { get; set; }
    public int? Inherited { get; set; }
    public int? Redirected { get; set; }
    public int? Deactivated { get; set; }
    public int? Reactivated { get; set; }
    public int? ProfilesNew { get; set; }
    public int? ProfilesReassigned { get; set; }
    public int? ProfilesRehashed { get; set; }
    public int? ProfilesRemoved { get; set; }
    public int? ArtifactsPending { get; set; }

    /// <summary>
    /// Auto-merge edges the run recorded (see <see cref="IdentityEdge"/>). Nullable rather than
    /// defaulting to 0 because runs that predate case evidence have no value for it, and 0 would read
    /// as "this run merged nothing".
    ///
    /// <para>Modeled here, not only in the engine's bootstrap: the engine's ResolveRun update binds
    /// this column, so a host whose migrations created the registry without it fails the resolve
    /// outright on an invalid column name. Caught by diffing a host-migrated schema against a
    /// bootstrapped one — the two must agree or the deploy path and the dev loop build different
    /// databases.</para>
    /// </summary>
    public int? AutoMergeEdges { get; set; }

    public string? Notes { get; set; }
}
