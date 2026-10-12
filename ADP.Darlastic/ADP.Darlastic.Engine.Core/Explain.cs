namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Which rules participated in a pair's score. Set as cheap bit-ors on the SAME code path that
/// computes the score (RealMatcher.ScoreCore), so flag truth can never drift from score truth.
/// The case browser's category index is built from these during the cluster walk — no trace
/// allocation in the 12.4M-pair hot loop.
/// </summary>
[Flags]
public enum MatchFlags : long   // long: persisted as BIGINT; values below bit 31 are unchanged
{
    None = 0,
    NamesBoth        = 1 << 0,
    PhonesBoth       = 1 << 1,
    PhoneExact       = 1 << 2,
    PhoneSuffix      = 1 << 3,   // weak↔strong suffix-8 bridge
    IdsBoth          = 1 << 4,
    IdEqual          = 1 << 5,
    IdConflict       = 1 << 6,   // ×0.3 fired
    NameOnlyDamp     = 1 << 7,   // ×0.70 fired (name is the only signal)
    NameConsistent   = 1 << 8,   // exact phone + per-token-aligned names → 0.93 floor fired
    NameConflictCap  = 1 << 9,   // exact phone + unrelated names → 0.55 cap fired
    AddressBoost     = 1 << 10,  // district-level address agreement → +0.05 fired
    ChainSliceRescue = 1 << 11,  // same given + exact phone + district remainder → raised to 0.91
    DobEqual         = 1 << 12,  // +0.05 fired
    DobConflict      = 1 << 13,  // ×0.6 fired
    PhoneNameMerge   = 1 << 14,  // exact phone + overlapping (not fully consistent) name, no gender/total conflict → 0.91 (name-chain-slice rule)
    GenderConflict   = 1 << 15,  // exact phone but male vs female → capped (spouses/family on one line)
    VinBoth          = 1 << 16,  // both sides carry ≥1 VIN link (a comparison was possible)
    VinSoldOverlap   = 1 << 17,  // shared VIN, ≥1 sale side, ownership periods consistent — strong ownership key
    VinServiced      = 1 << 18,  // shared VIN, service-only on both sides — corroboration grade
    VinTransfer      = 1 << 19,  // shared VIN but disjoint periods with a sale between — ownership changed, NOT a merge signal (P7)
    VinSoldMerge     = 1 << 20,  // sold-overlap floor fired → 0.91 auto-merge (names not in hard conflict)
    OrgLine          = 1 << 21,  // exact phone + a business/placeholder name (Company/Stock/Parts/…) — person-merge floor withheld (NOT a veto; identical org names still merge on the base)
    SameAsRef        = 1 << 22,  // one side carries a source-asserted reference to the other (explicit FK, e.g. ticket → DMS customer key)
    SameAsMerge      = 1 << 23,  // same-as floor fired → auto-merge (names consistent/aligned or absent)
    EmailsBoth       = 1 << 24,  // both sides carry >=1 match-eligible (canonical, non-role) e-mail — a comparison was possible
    EmailExact       = 1 << 25,  // a canonical address is shared → 1.00 (weight 0.45); never a penalty when they differ
    EmailMerge       = 1 << 26,  // shared e-mail + names consistent/aligned (or absent) → auto-merge floor fired
    EmailRoleOnly    = 1 << 27,  // a side's only e-mail was a role/shared mailbox (info@, sales@…) — suppressed from matching, still survives to golden
    GivenNameConflict  = 1 << 28,  // Flags.StructuredNames: both given names known and they do not align; or Flags.DistinctGivenNames: different first names on a shared phone → no floor, held below the steward line
    FamilyNameConflict = 1 << 29,  // Flags.StructuredNames: both family names known and they do not align → given-only floors withheld, held below the line
    NamelessHeld       = 1 << 30,  // Flags.NamelessNeedsCorroboration: a nameless pair's single key (phone alone, or a sold VIN against differing phones) held below the line
    ServicedVinNameMerge = 1L << 31, // Flags.ServicedVinNameFloor: shared serviced VIN + fully consistent names of 2+ words → floor 0.91 (phones may differ)
}

/// <summary>What normalization did to one record, before any comparison happened.</summary>
/// <param name="Stage">repair | arabizi | name | phone | address.</param>
/// <param name="Detail">Human-readable before → after.</param>
public sealed record NormalizeStep(string Stage, string Detail);

/// <summary>
/// The kinds of personal value an explanation quotes from the records it describes. A reader who
/// may not see them gets the explanation with each one written as its kind allows (see
/// <see cref="ValueQuoter"/>), so the reasoning reads the same and the values do not leak.
/// </summary>
public enum QuotedValue
{
    Name,
    Phone,
    Email,
    IdNumber,
    BirthDate,
    Address,

    /// <summary>The city slot of an address. A reader's quoter usually writes it as recorded (the city is
    /// shown beside the record); one that shows only cities enough customers share masks the rest.</summary>
    City,
}

/// <summary>
/// Writes one record value into an explanation, as its reader may see it. Called with the exact
/// value the explanation would otherwise print, never with text around it, so a value containing
/// quotes, separators or any other punctuation is replaced whole.
/// </summary>
public delegate string ValueQuoter(QuotedValue kind, string value);

/// <summary>
/// The record-level half of the explanation: how raw source text became the text the matcher
/// compares. Lives in the Engine rather than a browsing surface for the same reason the category
/// rules do — it describes what the normalizer actually did, and a copy living next to a UI would
/// eventually describe a transformation the engine no longer performs.
/// </summary>
public static class Normalization
{
    public static List<NormalizeStep> Steps(RealRecord r) => Steps(r, null);

    /// <summary>
    /// The steps, with every record value written through <paramref name="quote"/>. Without one the
    /// values appear as recorded. The city goes through the quoter as <see cref="QuotedValue.City"/>,
    /// so it reads the way the record view beside it shows the city.
    /// </summary>
    public static List<NormalizeStep> Steps(RealRecord r, ValueQuoter? quote)
    {
        string Q(QuotedValue kind, string value) => quote is null ? value : quote(kind, value);

        var steps = new List<NormalizeStep>();
        if (r.RawName.Length > 0)
        {
            if (r.NameWasMojibake)
                steps.Add(new("repair", $"garbled text repaired: \"{Q(QuotedValue.Name, r.RawName)}\" → \"{Q(QuotedValue.Name, Mojibake.TryRepair(r.RawName) ?? "")}\", then written in Latin letters"));
            if (r.NameHadArabizi)
                steps.Add(new("arabizi", "digits used as letters in chat-style writing (3→a · 7→h · 5→kh …) turned back into letters before writing in Latin letters"));
            steps.Add(new("name", r.NormName.Length > 0
                ? $"\"{Q(QuotedValue.Name, r.RawName)}\" → \"{Q(QuotedValue.Name, r.NormName)}\""
                : $"\"{Q(QuotedValue.Name, r.RawName)}\" → dropped (not a usable name)"));
        }
        else steps.Add(new("name", "no name in source"));

        if (r.Phones.Length + r.WeakPhones.Length > 0)
            steps.Add(new("phone", string.Join(" · ",
                r.Phones.Select(p => Q(QuotedValue.Phone, p) + " (full number)")
                    .Concat(r.WeakPhones.Select(p => Q(QuotedValue.Phone, p) + " (incomplete number)")))));

        if (r.NormAddress.Length > 0)
            steps.Add(new("address", $"\"{Q(QuotedValue.Address, r.RawAddress.Replace('|', ' '))}\" → \"{Q(QuotedValue.Address, r.NormAddress)}\" · city \"{Q(QuotedValue.City, r.NormCity)}\""));

        return steps;
    }
}

/// <summary>One step of a pair's scoring story, in engine execution order.</summary>
/// <param name="Stage">Machine key the UI maps to a station: signal | base | gate | address | conflict | decide.</param>
/// <param name="Title">Short plain-language label ("Names", "Same number, same first name").</param>
/// <param name="Detail">What the step found, in words a reader of the case browser understands; no
/// rule codes or thresholds (the running strength is <paramref name="ConfAfter"/>).</param>
/// <param name="ConfAfter">Running confidence after this step, when it changed or concluded a phase.</param>
public sealed record TraceStep(string Stage, string Title, string Detail, double? ConfAfter = null);

/// <summary>
/// Trace sink for explain-mode scoring. Passed as null in every hot path (zero overhead);
/// allocated only when the case browser explains ONE pair on demand. The trace is generated by
/// the live scorer itself — the single-source-of-truth principle of `dotnet run eval` extended
/// to explanations: the browser can never claim something the engine didn't do.
/// </summary>
public sealed class MatchTrace
{
    private readonly ValueQuoter? quote;

    public MatchTrace() { }

    /// <summary>A trace whose steps write every record value they quote through <paramref name="quote"/>.</summary>
    public MatchTrace(ValueQuoter? quote) => this.quote = quote;

    public readonly List<TraceStep> Steps = [];
    public void Add(string stage, string title, string detail, double? confAfter = null)
        => Steps.Add(new TraceStep(stage, title, detail, confAfter));

    /// <summary>A record value as this trace's reader may see it: as recorded, unless the trace was given a quoter.</summary>
    public string Quote(QuotedValue kind, string value) => quote is null ? value : quote(kind, value);
}
