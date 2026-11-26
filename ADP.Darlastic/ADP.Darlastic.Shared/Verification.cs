namespace ShiftSoftware.ADP.Darlastic.Shared;

/// <summary>
/// The kinds of claim the module knows out of the box. A host may configure others; a kind is a name plus its
/// settings, nothing in the registry is specific to these three.
/// </summary>
public static class VerificationKind
{
    /// <summary>The customer is who they say they are (an identity document, a staff check).</summary>
    public const string Person = "person";

    /// <summary>A phone number is this customer's.</summary>
    public const string Phone = "phone";

    /// <summary>A vehicle (by VIN) is this customer's.</summary>
    public const string Vehicle = "vehicle";
}

/// <summary>What a host reports about one verification attempt.</summary>
public static class VerificationOutcome
{
    /// <summary>The claim was proven: it holds from now on.</summary>
    public const string Verified = "verified";

    /// <summary>The check was done, but by a method that does not prove the claim by itself (a document looked
    /// at by a person, a photo of it): it waits for a review, which moves it to verified or rejects it.</summary>
    public const string Submitted = "submitted";

    /// <summary>Written by the module, never reported: the value belongs to one customer for good and another
    /// customer already holds it, so this attempt waits for a review and changes nothing until then.</summary>
    public const string Held = "held";

    /// <summary>Written by a review: a submitted or held attempt was turned down.</summary>
    public const string Rejected = "rejected";

    /// <summary>The attempt did not prove the claim. Logged; changes nothing.</summary>
    public const string Failed = "failed";

    /// <summary>A claim that held is withdrawn.</summary>
    public const string Revoked = "revoked";

    /// <summary>An outcome a host may report (held and rejected are the module's own).</summary>
    public static bool IsKnown(string? outcome) => outcome is Verified or Submitted or Failed or Revoked;
}

/// <summary>Why a verified claim stopped holding.</summary>
public static class VerificationEndReason
{
    /// <summary>The same value was verified for another customer (exclusive kinds).</summary>
    public const string Superseded = "superseded";

    /// <summary>The same claim was verified again for the same customer: a new period starts.</summary>
    public const string Renewed = "renewed";

    /// <summary>Withdrawn by a person.</summary>
    public const string Revoked = "revoked";

    /// <summary>A review approved it: the verified period that follows is a claim of its own.</summary>
    public const string Promoted = "promoted";

    /// <summary>A review turned it down.</summary>
    public const string Rejected = "rejected";
}

/// <summary>
/// How far a claim has come. Only <see cref="Verified"/> is proof; the two below it wait for a review (a
/// person in the back office, or a host's automated check) that moves the claim up or rejects it.
/// </summary>
public static class VerificationLevel
{
    /// <summary>Proven.</summary>
    public const string Verified = "verified";

    /// <summary>Checked by a method that needs a review before it counts as proof.</summary>
    public const string Submitted = "submitted";

    /// <summary>Not counted at all until a review: the value is one another customer already holds, and it
    /// can belong to one customer only.</summary>
    public const string Held = "held";

    /// <summary>Higher is stronger; a held or unknown level is 0 and meets no requirement.</summary>
    public static int Rank(string? level) => level switch
    {
        Verified => 2,
        Submitted => 1,
        _ => 0,
    };

    /// <summary>A level a rule may ask for.</summary>
    public static bool CanBeRequired(string? level) => level is Verified or Submitted;
}
