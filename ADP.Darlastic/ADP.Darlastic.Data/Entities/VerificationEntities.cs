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

    /// <summary>What kind of value it is, for a kind whose values come in types (an identity document: national
    /// ID, passport ...); empty otherwise. Part of what identifies the value.</summary>
    public string ValueType { get; set; } = "";

    /// <summary>The host's name for how it verified.</summary>
    public string Method { get; set; } = default!;

    /// <summary>verified / submitted / held / failed / revoked / rejected.</summary>
    public string Outcome { get; set; } = default!;

    /// <summary>The claim a review decided on, when this row is a review's decision (its actor is the reviewer).</summary>
    public long? ReviewOfClaimID { get; set; }

    /// <summary>The reviewer's words, when they left any.</summary>
    public string? Note { get; set; }

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
/// <para>A claim below the verified level (<see cref="Level"/>: submitted, or held because another customer
/// has the value) is a row here too, open while it waits for a review. A review that approves it ends it
/// (promoted) and opens the verified period as a new row.</para>
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

    /// <summary>The value's type, for a kind whose values come in types; empty otherwise.</summary>
    public string ValueType { get; set; } = "";

    /// <summary>verified / submitted / held.</summary>
    public string Level { get; set; } = "verified";

    /// <summary>Held only: the other customer's claim on the same value that this one ran into.</summary>
    public long? ConflictClaimID { get; set; }

    /// <summary>At most one open row with this set may exist per value (the unique index filters on it): a
    /// verified claim of an exclusive kind, and a verified or submitted claim of a kind whose values belong to
    /// one customer for good.</summary>
    public bool Exclusive { get; set; }

    public string Method { get; set; } = default!;
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    /// <summary>superseded / renewed / revoked / promoted / rejected.</summary>
    public string? EndReason { get; set; }

    public long StartedByEntryID { get; set; }
    public VerificationEntry StartedBy { get; set; } = default!;

    public long? EndedByEntryID { get; set; }
    public VerificationEntry? EndedBy { get; set; }

    public string? Location { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }
}
