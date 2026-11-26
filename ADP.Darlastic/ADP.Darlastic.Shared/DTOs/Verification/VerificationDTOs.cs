namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.Verification;

/// <summary>One verification attempt a host reports (see <see cref="VerificationOutcome"/>).</summary>
public class VerificationReportDTO
{
    /// <summary>The golden customer the claim is about. A merged customer's old number is followed to the live one.</summary>
    public string IdentityID { get; set; } = default!;

    /// <summary>A configured kind (<see cref="VerificationKind"/> or one of the host's).</summary>
    public string Kind { get; set; } = default!;

    /// <summary>What the claim is about: the phone number, the VIN. Normalized by the kind's settings.</summary>
    public string? Value { get; set; }

    /// <summary>The value's type, for a kind whose values come in types (a document: national ID, passport ...).</summary>
    public string? ValueType { get; set; }

    /// <summary>Revoking only: the open claim to end, by its number, instead of naming its value again.</summary>
    public long? ClaimID { get; set; }

    /// <summary>The host's name for how it was verified ("otp-sms", "nfc", "staff-check" ...).</summary>
    public string Method { get; set; } = default!;

    /// <summary><see cref="VerificationOutcome"/>. Absent means verified.</summary>
    public string? Outcome { get; set; }

    /// <summary>Where it happened, in the host's words (a dealer or branch).</summary>
    public string? Location { get; set; }

    /// <summary>The source record the visit was working with, when there was one.</summary>
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }

    /// <summary>The host's own reference to its evidence (an OTP session, a scan). Never the evidence itself.</summary>
    public string? EvidenceReference { get; set; }
}

/// <summary>What recording a report did.</summary>
public class VerificationResultDTO
{
    /// <summary>The log row.</summary>
    public long EntryID { get; set; }

    /// <summary>The live customer the report was recorded for.</summary>
    public string IdentityID { get; set; } = default!;

    public string Kind { get; set; } = default!;
    public string Outcome { get; set; } = default!;

    /// <summary>The claim the report opened; null for a failed or revoking report.</summary>
    public long? ClaimID { get; set; }

    /// <summary>The level of that claim (<see cref="VerificationLevel"/>), or of the claim the customer already
    /// had on the value when the report changed nothing.</summary>
    public string? Level { get; set; }

    /// <summary>The value belongs to one customer and another already holds it: the attempt waits for a
    /// review. Never says who.</summary>
    public bool Held { get; set; }

    /// <summary>Claims this report ended, each with the customer that held it and why it ended.</summary>
    public List<VerificationEndedClaimDTO> Ended { get; set; } = [];
}

public class VerificationEndedClaimDTO
{
    public long ClaimID { get; set; }
    public string IdentityID { get; set; } = default!;
    public string Reason { get; set; } = default!;
}

/// <summary>A period in which a claim held (or holds) for a customer.</summary>
public class VerifiedClaimDTO
{
    public long ClaimID { get; set; }

    /// <summary>The customer number the claim was recorded on (a merged customer's old number stays as recorded).</summary>
    public string IdentityID { get; set; } = default!;

    public string Kind { get; set; } = default!;

    /// <summary>The value as this caller may see it; empty for a kind without one.</summary>
    public string? Value { get; set; }

    /// <summary>The value's type, for a kind whose values come in types; null otherwise.</summary>
    public string? ValueType { get; set; }

    /// <summary><see cref="VerificationLevel"/>: verified, submitted or held.</summary>
    public string Level { get; set; } = VerificationLevel.Verified;

    /// <summary>Open below the verified level: waiting for a review.</summary>
    public bool Pending { get; set; }

    public string Method { get; set; } = default!;
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public string? EndReason { get; set; }

    /// <summary>When the kind's validity ends this claim; null when it never expires.</summary>
    public DateTime? ExpiresUtc { get; set; }

    /// <summary>Holds now: verified, not ended and not expired.</summary>
    public bool Holds { get; set; }

    public string? Location { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceRecordId { get; set; }
}

/// <summary>A customer's verification: every claim that holds or waits for a review, and with history the
/// periods that ended.</summary>
public class CustomerVerificationDTO
{
    /// <summary>The live customer (after following a merge).</summary>
    public string IdentityID { get; set; } = default!;

    public List<VerifiedClaimDTO> Claims { get; set; } = [];
}

/// <summary>"Is this customer verified for this visit?" The values are the visit's own (its phone, its VIN).</summary>
public class VerificationStatusRequestDTO
{
    public string IdentityID { get; set; } = default!;

    /// <summary>Which of the host's rules to answer; absent is the default rule.</summary>
    public string? Context { get; set; }

    /// <summary>The visit's value per kind, for requirements that ask for a specific value.</summary>
    public Dictionary<string, string>? Values { get; set; }
}

public class VerificationStatusDTO
{
    public string IdentityID { get; set; } = default!;
    public string Context { get; set; } = default!;

    /// <summary>Every requirement of the rule is met.</summary>
    public bool Verified { get; set; }

    public List<VerificationRequirementStatusDTO> Requirements { get; set; } = [];
}

public class VerificationRequirementStatusDTO
{
    public string Kind { get; set; } = default!;

    /// <summary>The rule asks for the visit's own value of this kind, not any value.</summary>
    public bool SpecificValue { get; set; }

    public bool Met { get; set; }

    /// <summary>Why not, in a word: "not-verified", "pending-review" (a claim is open below the level the rule
    /// asks for), "value-missing" (the visit gave no value of this kind) or "value-invalid". Never whether
    /// another customer holds the value.</summary>
    public string? Missing { get; set; }

    /// <summary>The level the rule asks for.</summary>
    public string RequiredLevel { get; set; } = VerificationLevel.Verified;

    /// <summary>The level the customer's strongest claim for this requirement has; null with none.</summary>
    public string? Level { get; set; }

    /// <summary>The claim that meets it, or the one still below the level asked for.</summary>
    public long? ClaimID { get; set; }
    public string? Method { get; set; }
    public DateTime? SinceUtc { get; set; }
}

/// <summary>The claims that wait for a review, oldest first.</summary>
public class VerificationReviewQueueDTO
{
    /// <summary>How many wait in all.</summary>
    public int Total { get; set; }

    public List<VerificationReviewItemDTO> Items { get; set; } = [];
}

/// <summary>One claim waiting for a review: submitted under a method that needs one, or held because the value
/// belongs to one customer and another has it.</summary>
public class VerificationReviewItemDTO
{
    public long ClaimID { get; set; }

    /// <summary>The customer the claim is for, as they are numbered now; null when they no longer exist.</summary>
    public string? IdentityID { get; set; }

    public string Kind { get; set; } = default!;
    public string? ValueType { get; set; }

    /// <summary>The value as this caller may see it; null for a hashed value.</summary>
    public string? Value { get; set; }

    /// <summary>submitted or held.</summary>
    public string Level { get; set; } = default!;

    public string Method { get; set; } = default!;
    public DateTime SubmittedUtc { get; set; }
    public string SubmittedBy { get; set; } = default!;
    public string? Location { get; set; }

    /// <summary>The host's reference to its own evidence for the attempt.</summary>
    public string? EvidenceReference { get; set; }

    /// <summary>Held: the customer that holds the value, as numbered now.</summary>
    public string? ConflictIdentityID { get; set; }

    /// <summary>Held: the other claim, its level and since when; null once that claim ended.</summary>
    public long? ConflictClaimID { get; set; }
    public string? ConflictLevel { get; set; }
    public DateTime? ConflictSinceUtc { get; set; }

    /// <summary>Held: the other claim still stands for a different customer, so approving is refused. False
    /// once the two were merged or the other claim ended.</summary>
    public bool ConflictOpen { get; set; }
}

/// <summary>A reviewer's decision on a claim that waits.</summary>
public class VerificationReviewDecisionDTO
{
    public long ClaimID { get; set; }

    /// <summary>True moves it to verified; false rejects it.</summary>
    public bool Approve { get; set; }

    public string? Note { get; set; }
}
