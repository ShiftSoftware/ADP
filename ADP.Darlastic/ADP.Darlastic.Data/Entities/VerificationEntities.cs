namespace ShiftSoftware.ADP.Darlastic.Data.Entities;

/// <summary>
/// One verification attempt a host reported: who was verified, for what, how, with what outcome, by whom and
/// where. Append-only; the audit trail behind <see cref="VerifiedClaim"/>.
///
/// <para>Verification is a separate concept from unification. A resolve decides which source records are one
/// customer; a verification records that someone proved a claim about a customer (this is the person, this
/// phone is theirs, this vehicle is theirs). No resolve reads or writes these tables, and nothing here
/// changes which records are merged.</para>
/// </summary>
public class VerificationEntry
{
    public long EntryID { get; set; }
    public DateTime AtUtc { get; set; }
    public string Actor { get; set; } = default!;

    /// <summary>The live golden customer the report was recorded for.</summary>
    public long IdentityID { get; set; }

    public string Kind { get; set; } = default!;

    /// <summary>The normalized value the claim is about (a phone key, a VIN, a keyed hash of a document
    /// number); empty for a kind without a value.</summary>
    public string ValueKey { get; set; } = default!;

    /// <summary>The host's name for how it verified.</summary>
    public string Method { get; set; } = default!;

    /// <summary>verified / failed / revoked.</summary>
    public string Outcome { get; set; } = default!;

    /// <summary>Where, in the host's words (a dealer or branch).</summary>
    public string? Location { get; set; }

    /// <summary>The source record the visit was working with, when there was one.</summary>
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }

    /// <summary>The host's reference to its own evidence. Never the evidence.</summary>
    public string? EvidenceReference { get; set; }
}

/// <summary>
/// One period in which a claim held for a customer. Open (<see cref="EndUtc"/> null) while it holds; a row is
/// never deleted, so the history reads "verified for A from March to July, then for B".
///
/// <para>For an exclusive kind at most one open row exists per (kind, value), enforced by a unique filtered
/// index: two desks verifying one phone for two customers at the same moment cannot both win.</para>
/// </summary>
public class VerifiedClaim
{
    public long ClaimID { get; set; }

    /// <summary>The customer number the claim was recorded on. After a merge the retired number stays here and
    /// readers follow the redirect, as every other read of the registry does.</summary>
    public long IdentityID { get; set; }

    public string Kind { get; set; } = default!;
    public string ValueKey { get; set; } = default!;

    /// <summary>The kind was exclusive when this was recorded (the unique index filters on it).</summary>
    public bool Exclusive { get; set; }

    public string Method { get; set; } = default!;
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    /// <summary>superseded / renewed / revoked.</summary>
    public string? EndReason { get; set; }

    public long StartedByEntryID { get; set; }
    public VerificationEntry StartedBy { get; set; } = default!;

    public long? EndedByEntryID { get; set; }
    public VerificationEntry? EndedBy { get; set; }

    public string? Location { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }
}
