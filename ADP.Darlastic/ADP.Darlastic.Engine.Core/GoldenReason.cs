namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Why a golden customer looks the way it does, in terms a person reading a customer list can act
/// on: how its records were combined, and what about it may need a second look.
///
/// <para><b>Bit layout.</b> These share one staged column with <see cref="ClusterCat"/>
/// (<c>IdentitySummary.Categories</c>): cluster categories keep the low 32 bits, reasons take the
/// high 32. The first six reasons are the cluster categories themselves, shifted by
/// <see cref="GoldenReasons.ClusterCatShift"/>, so a reader of a registry staged without reasons can
/// still derive those six from the cluster bits alone, and a reader that only knows
/// <see cref="ClusterCat"/> keeps working unchanged against a registry staged with them.</para>
///
/// <para>Computed by the resolve, because most of these are facts about pairs and only the resolve's
/// pair walk sees every pair. Staged only when the resolve is asked for per-golden evidence
/// (<c>Merge.ClusterFromBlocks(perGoldenEvidence: true)</c>); without it nothing here is written and
/// the staged state is exactly what it was before reasons existed.</para>
/// </summary>
[Flags]
public enum GoldenReason : long
{
    None = 0,

    // ---- the cluster categories, shifted (bit = ClusterCat bit + 32) -------------------------
    NameCompleted        = 1L << 32,   // ClusterCat.LongestChain
    PhoneFromShortNumber = 1L << 33,   // ClusterCat.WeakPhoneFallback
    SeveralSources       = 1L << 34,   // ClusterCat.CrossDealer
    LargeGroup           = 1L << 35,   // ClusterCat.Large
    MergedByVehicle      = 1L << 36,   // ClusterCat.VinBridged
    LinkedOnlyByVehicle  = 1L << 37,   // ClusterCat.VinDecisive

    // ---- resolve-only ----------------------------------------------------------------------
    SingleRecord            = 1L << 38,   // exactly one record
    MergedByPhoneAndName    = 1L << 39,   // an auto-merge edge on the exact-phone + agreeing-name floors
    MergedByEmail           = 1L << 40,   // an auto-merge edge on the shared e-mail floor
    MergedByReference       = 1L << 41,   // an auto-merge edge on a source-asserted reference
    MergedBySimilarDetails  = 1L << 42,   // an auto-merge edge that cleared the line on the weighted score alone
    MergedByReviewer        = 1L << 43,   // a steward "same" verdict joined two of its records
    NeedsReview             = 1L << 44,   // a member sits in the steward queue
    PossibleDuplicate       = 1L << 45,   // a member's near-miss partner is in another customer
    SamePhoneDifferentNames = 1L << 46,   // a member shares an exact phone with a clearly different name
    CompanyOrFleet          = 1L << 47,   // a member is an organisation/placeholder record or on an org line
    VehicleChangedOwner     = 1L << 48,   // a member's vehicle passed between owners
    SameVehicleSimilarName  = 1L << 49,   // a member shares a purchased vehicle with a same-first-name customer elsewhere

    // ---- reader-side ---------------------------------------------------------------------
    /// <summary>Never staged by the resolve. A reader sets it for a golden whose identity was not in
    /// the latest run at all (every one of its records comes from a source that run did not load),
    /// so its staged summary is missing rather than empty.</summary>
    NotInLatestRun = 1L << 50,

    // ---- resolve-only, added after the reader-side bit (bits are persisted, so never renumbered) ----
    MergedByServicedVehicleAndName = 1L << 51,   // an auto-merge edge on the serviced-vehicle + same-full-name floor (Flags.ServicedVinNameFloor)
}

/// <summary>The rules that turn the resolve's pair walk into <see cref="GoldenReason"/>s.</summary>
public static class GoldenReasons
{
    /// <summary><see cref="ClusterCat"/> bit n is <see cref="GoldenReason"/> bit n + 32.</summary>
    public const int ClusterCatShift = 32;

    /// <summary>The low half of a staged categories value: the cluster categories.</summary>
    public const long ClusterCatMask = 0xFFFF_FFFFL;

    /// <summary>Every reason, in declaration order — the canonical list for chips and counts.
    /// Excludes <see cref="GoldenReason.None"/>.</summary>
    public static readonly GoldenReason[] All =
        [.. Enum.GetValues<GoldenReason>().Where(r => r != GoldenReason.None)];

    /// <summary>Every reason bit OR'd together. A staged value with any of these set was staged with
    /// per-golden evidence.</summary>
    public static readonly long AllMask = All.Aggregate(0L, (m, r) => m | (long)r);

    /// <summary>The pair categories that say something about the CUSTOMERS a pair's two records
    /// belong to, beyond how they were combined. Pairs carrying none of these are not kept for
    /// reason computation.</summary>
    public const CaseCat RelationshipCats =
        CaseCat.NearMiss | CaseCat.ConflictVetoed | CaseCat.OrgLine | CaseCat.VinTransfer | CaseCat.VinReview;

    /// <summary>The reasons a set of cluster categories implies — the shifted mirror bits.</summary>
    public static GoldenReason FromClusterCats(ClusterCat cats) => (GoldenReason)((long)cats << ClusterCatShift);

    /// <summary>The cluster categories in a staged categories value.</summary>
    public static ClusterCat ClusterCatsOf(long staged) => (ClusterCat)(staged & ClusterCatMask);

    /// <summary>The reasons in a staged categories value.</summary>
    public static GoldenReason ReasonsOf(long staged) => (GoldenReason)(staged & AllMask);

    /// <summary>One staged value: cluster categories in the low half, reasons in the high half.</summary>
    public static long Pack(ClusterCat cats, GoldenReason reasons) => (long)cats | (long)reasons;

    /// <summary>
    /// How one auto-merge edge was justified, by the floor that fired. An edge on which several
    /// floors fired carries all of them: each one alone would have merged it, so each is a true
    /// answer to "why were these combined".
    /// </summary>
    public static GoldenReason OfMergeEdge(MatchFlags f)
    {
        var r = GoldenReason.None;
        if ((f & MatchFlags.VinSoldMerge) != 0) r |= GoldenReason.MergedByVehicle;
        if ((f & MatchFlags.EmailMerge) != 0) r |= GoldenReason.MergedByEmail;
        if ((f & MatchFlags.SameAsMerge) != 0) r |= GoldenReason.MergedByReference;
        if ((f & MatchFlags.ServicedVinNameMerge) != 0) r |= GoldenReason.MergedByServicedVehicleAndName;
        if ((f & MatchFlags.PhoneExact) != 0
            && (f & (MatchFlags.NameConsistent | MatchFlags.PhoneNameMerge | MatchFlags.ChainSliceRescue)) != 0)
            r |= GoldenReason.MergedByPhoneAndName;
        return r == GoldenReason.None ? GoldenReason.MergedBySimilarDetails : r;
    }

    /// <summary>
    /// What one categorised pair says about the customers its records ended up in.
    /// <paramref name="sameCustomer"/> is whether the final clustering put both records in one
    /// identity — a near miss inside one customer is not a duplicate of anyone.
    /// </summary>
    public static GoldenReason OfPair(CaseCat cats, bool sameCustomer)
    {
        var r = GoldenReason.None;
        if ((cats & CaseCat.ConflictVetoed) != 0) r |= GoldenReason.SamePhoneDifferentNames;
        if ((cats & CaseCat.OrgLine) != 0) r |= GoldenReason.CompanyOrFleet;
        if ((cats & CaseCat.VinTransfer) != 0) r |= GoldenReason.VehicleChangedOwner;
        if (!sameCustomer)
        {
            if ((cats & CaseCat.NearMiss) != 0) r |= GoldenReason.PossibleDuplicate;
            if ((cats & CaseCat.VinReview) != 0) r |= GoldenReason.SameVehicleSimilarName;
        }
        return r;
    }

    /// <summary>
    /// A golden's reasons from what was staged for it, for a reader. <paramref name="staged"/> is
    /// the identity's staged categories value, or null when the identity has no staged summary.
    /// <paramref name="reasonsStaged"/> says whether the latest run staged per-golden evidence at all.
    /// <list type="bullet">
    /// <item>Staged with evidence: the staged reasons.</item>
    /// <item>Staged without evidence (multi-record identities only): the mirrored cluster reasons.</item>
    /// <item>No summary: a single record is a single record either way; with evidence on, a missing
    /// summary means the identity was not in the latest run.</item>
    /// </list>
    /// </summary>
    public static GoldenReason ForReader(long? staged, int memberCount, bool reasonsStaged)
    {
        if (staged is { } s)
            return (s & AllMask) != 0 ? ReasonsOf(s) : FromClusterCats(ClusterCatsOf(s));
        var r = memberCount == 1 ? GoldenReason.SingleRecord : GoldenReason.None;
        return reasonsStaged ? r | GoldenReason.NotInLatestRun : r;
    }
}
