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
/// The normalized record behind a queued pair, stored as JSON so a steward surface can render the
/// evidence without re-reading (or re-normalizing) the source systems. Rewritten with the queue.
/// </summary>
public class StewardRecord
{
    public string SourceSystem { get; set; } = default!;
    public string SourceRecordId { get; set; } = default!;
    public int RunID { get; set; }
    public string Payload { get; set; } = default!;
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
    public string? Notes { get; set; }
}
