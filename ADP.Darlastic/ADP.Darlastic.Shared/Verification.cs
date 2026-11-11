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

    /// <summary>The attempt did not prove the claim. Logged; changes nothing.</summary>
    public const string Failed = "failed";

    /// <summary>A claim that held is withdrawn.</summary>
    public const string Revoked = "revoked";

    public static bool IsKnown(string? outcome) => outcome is Verified or Failed or Revoked;
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
}
