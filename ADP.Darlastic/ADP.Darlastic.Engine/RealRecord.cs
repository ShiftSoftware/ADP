namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Parsed DOB that preserves day/month ambiguity instead of guessing.
/// Real tenant data mixes m/d/yyyy and d/m/yyyy with no zero-padding; ~19K rows carry
/// registration dates (2025/2026) entered as DOB. Equality/conflict logic must be
/// conservative or it poisons matching (see profiling, 2026-06-07).
/// </summary>
public sealed record DobInfo(int Year, int P1, int P2)
{
    /// <summary>1/1/yyyy is a common "unknown day" placeholder — never use as conflict evidence.</summary>
    public bool IsPlaceholder => P1 == 1 && P2 == 1;

    /// <summary>Same date under at least one consistent day/month reading of both sides.</summary>
    public bool EqualsLoose(DobInfo other) =>
        Year == other.Year &&
        ((P1 == other.P1 && P2 == other.P2) || (P1 == other.P2 && P2 == other.P1));

    /// <summary>Definitely different dates (no reading reconciles them). Both sides must be non-placeholder.</summary>
    public bool ConflictsWith(DobInfo other) => !EqualsLoose(other) && !IsPlaceholder && !other.IsPlaceholder;
}

/// <summary>One real dealer-DMS customer row after normalization (the V0 source-profile record; the generalized SourceRecord contract supersedes it).</summary>
public sealed record RealRecord(
    int Idx,                  // dense index into the in-memory record array
    string SourceSystem,      // dealer slug from the file name
    string SourceRecordId,    // MagicNumber (unique per file per profiling)
    string RawName,
    string NormName,          // "" when blocklisted / mojibake / junk — treat as missing
    string[] Phones,          // normalized valid in-country mobiles (tenant rule: 10 digits starting '7')
    string[] WeakPhones,      // 9-digit '7'-prefixed (one dealer's missing-digit +964 export form) — suffix-match only
    string? NationalId,       // digits-only, length 11–13, plausibility-filtered; null otherwise
    DobInfo? Dob,             // null when absent or implausible year
    string RawAddress = "",   // surviving raw Address001..005 components, '|'-joined (round-trips through CSVs)
    string NormAddress = "",  // transliterated/normalized address text, junk + country tail dropped; "" = missing
    string NormCity = "",     // last surviving component ≈ city/governorate slot; "" = missing
    bool NameWasMojibake = false,  // NormName recovered from CP1256-as-CP1252 garble (case-browser category)
    bool NameHadArabizi = false,   // raw name carried chat-numeral letters that were folded (Nafi3 → nafia)
    string? Gender = null,    // "m"/"f" when the Gender column is a clean M/F; null otherwise (~75% filled, male-skewed)
    VinLink[]? VinLinks = null,   // VS-sale / Labor-service VIN ties, joined from VSDatas/SOLabordatas by (dealer,magic); null until VinIngest.AttachTo runs
    bool IsOrgPlaceholder = false,   // dealer-self / placeholder org (Parts Stock, the dealer's own name) — excluded from matching (BlockKeysOf yields nothing)
    string[]? Emails = null,      // normalized (lowercased, trimmed) e-mail addresses; ingested + survived, not yet a matching signal
    string[]? SameAsRefs = null); // source-asserted same-entity references to OTHER profiles, as "sourceSystem|sourceRecordId"
                                  // keys (e.g. a CRM ticket carrying the DMS customer key it was raised for). An explicit
                                  // FK the matcher treats as king-key-grade evidence — still name-gated, never blind.

/// <summary>Where a VIN↔customer link came from — they are NOT equal-strength (domain expert, 2026-06-22).
/// A vehicle SALE binds the VIN to the buyer at point of sale (high-accuracy entry); a SERVICE visit
/// is a walk-in and may record a relative/driver/new-owner, so it corroborates but never decides.</summary>
public enum VinSource { Sale, Service }

/// <summary>
/// One profile's tie to one VIN from one source, with the date range over which that source observed
/// it (sale invoice date; service create dates). The range is what the ownership-period gate (P7)
/// reads to tell "same owner, contemporaneous" from "ownership transfer, sale in between".
/// </summary>
public sealed record VinLink(string Vin, VinSource Source, DateOnly? First, DateOnly? Last);
