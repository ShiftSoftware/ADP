namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Browsing categories for a scored pair. A case can carry several; the band tags
/// (AutoMerged / StewardBand / NearMiss) are mutually exclusive.
///
/// <para>These live in the Engine, not in a browsing surface, because the resolve itself is the
/// only pass that sees every pair — 12.4M of them at TIQ scale. Categorising there and staging the
/// result is what lets a hosted surface serve real corpus numbers from a table instead of
/// rebuilding an in-memory index it cannot afford. Keeping the rules here also means there is
/// exactly one definition: a second copy in a UI project is how a category silently comes to mean
/// two different things.</para>
/// </summary>
[Flags]
public enum CaseCat
{
    None = 0,
    AutoMerged     = 1 << 0,  // score >= 0.90 — these pairs union into one identity
    StewardBand    = 1 << 1,  // 0.80–0.90 — the Phase 5 queue
    NearMiss       = 1 << 2,  // 0.70–0.80 — kept separate, close
    ConflictVetoed = 1 << 3,  // exact phone but a hard rule said two people (name conflict / ID)
    AddressRescued = 1 << 4,  // chain-slice rescue fired (district-confirmed, auto-merge 0.91)
    Increment      = 1 << 5,  // phone+given shape, no district confirm, below the line — the deferred decision
    Mojibake       = 1 << 6,  // a CP1256-recovered name participates
    Arabizi        = 1 << 7,  // a chat-numeral name participates
    VinSold        = 1 << 8,  // sold-VIN ownership floor fired → 0.91 auto-merge
    VinServiced    = 1 << 9,  // serviced-VIN corroboration participated (rides along on a relevant case)
    VinTransfer    = 1 << 10, // shared VIN gated OUT as an ownership transfer (P7) — verify the gate held
    VinReview      = 1 << 11, // sold VIN + given name aligns but rest differs — NOT merged; steward-review bucket
    OrgLine        = 1 << 12, // exact phone + a business/placeholder name — person-merge floor withheld
}

/// <summary>Cluster-level categories (identity view).</summary>
[Flags]
public enum ClusterCat
{
    None = 0,
    LongestChain      = 1 << 0, // golden name extended to a fuller chain than the most-attested spelling
    WeakPhoneFallback = 1 << 1, // golden phone survived from a weak 9-digit number
    CrossDealer       = 1 << 2, // members from >= 2 sources — the initiative's business case
    Large             = 1 << 3, // >= 5 records
    VinBridged        = 1 << 4, // >= 1 auto-merge edge fired on the sold-VIN floor
    VinDecisive       = 1 << 5, // a sold-VIN edge name+phone+address alone could NOT have merged
}

public static class CaseCategories
{
    /// <summary>Every category a pair can carry, in declaration order — the canonical list for
    /// count tables, sidebars and catalog caps. Excludes <see cref="CaseCat.None"/>.</summary>
    public static readonly CaseCat[] All =
        [.. Enum.GetValues<CaseCat>().Where(c => c != CaseCat.None)];

    /// <summary>
    /// Category tags from the score + the flags the live scorer set. Pure and allocation-free:
    /// it runs inside the resolve's 12.4M-pair walk, so it may only read the flags that walk
    /// already computed (they are cheap bit-ors on the same code path as the score) plus the two
    /// bounded name probes below.
    ///
    /// <para>The Increment shape re-applies FirstTokensAlign only on the bounded exact-phone
    /// subset (never in Score's own hot path): exact phone + aligned given name + names not
    /// consistent/conflicting + no district rescue + below the auto-merge line.</para>
    /// </summary>
    public static CaseCat Categorize(RealRecord ra, RealRecord rb, double s, MatchFlags f)
    {
        CaseCat cats = CaseCat.None;
        if (s >= 0.90) cats |= CaseCat.AutoMerged;
        else if (s >= 0.80) cats |= CaseCat.StewardBand;
        else if (s >= 0.70) cats |= CaseCat.NearMiss;

        bool phoneExact = (f & MatchFlags.PhoneExact) != 0;
        if ((f & MatchFlags.ChainSliceRescue) != 0) cats |= CaseCat.AddressRescued;
        // ConflictVetoed = a hard rule ACTUALLY held the pair below auto-merge, so it can never
        // co-occur with Auto-merged (the contradiction a steward spotted: 'Parts Stock'~'Parts
        // Stock' was tagged both). NameConflict caps to 0.55 and ID conflict ×0.3 — both land
        // < 0.90. DobConflict no longer penalizes (2026-06-22) and the org guard only WITHHOLDS
        // the person floor (identical org names still merge on the base) — neither is a veto.
        if (phoneExact && s < 0.90 && (f & (MatchFlags.NameConflictCap | MatchFlags.IdConflict)) != 0)
            cats |= CaseCat.ConflictVetoed;
        if ((f & MatchFlags.OrgLine) != 0) cats |= CaseCat.OrgLine;

        // VIN tags: the sold-VIN auto-merges are the recall wins to spot-check (incl. the
        // same-given-different-father watch-point); transfers are the P7 gate to verify. Serviced
        // corroboration rides along on an already-relevant case so service-VIN noise never grows
        // the catalog on its own.
        if ((f & MatchFlags.VinSoldMerge) != 0) cats |= CaseCat.VinSold;
        if ((f & MatchFlags.VinTransfer) != 0) cats |= CaseCat.VinTransfer;
        if ((f & MatchFlags.VinServiced) != 0 && cats != CaseCat.None) cats |= CaseCat.VinServiced;
        // Sold VIN that did NOT auto-merge but the given name aligns = the demoted given-only
        // bucket: a car shared by a same-given-name pair whose later tokens differ — chain slice,
        // or relatives/resale. The `s < 0.90` guard is what makes the label honest: VinSoldMerge==0
        // only means the *VIN floor* didn't fire — the pair can still auto-merge on another lever
        // (exact phone+given, chain-slice address rescue, both 0.91), and without the guard those
        // ride-alongs showed up tagged BOTH Auto-merged AND "VIN review (NOT merged)".
        if (s < 0.90 && (f & MatchFlags.VinSoldOverlap) != 0 && (f & MatchFlags.VinSoldMerge) == 0
            && (f & MatchFlags.NamesBoth) != 0 && RealMatcher.GivenNamesMatch(ra.NormName, rb.NormName))
            cats |= CaseCat.VinReview;

        if (phoneExact && s < 0.90
            && (f & MatchFlags.NamesBoth) != 0
            && (f & (MatchFlags.NameConsistent | MatchFlags.NameConflictCap | MatchFlags.ChainSliceRescue | MatchFlags.OrgLine)) == 0
            && RealMatcher.FirstTokensAlign(ra.NormName, rb.NormName))
            cats |= CaseCat.Increment;

        // Script-recovery tags ride along on cases interesting for another reason, plus any
        // exact-phone meeting of a recovered name (the "recovered but still unmatched" QA shape).
        bool moji = ra.NameWasMojibake || rb.NameWasMojibake;
        bool arz = ra.NameHadArabizi || rb.NameHadArabizi;
        if ((moji || arz) && (cats != CaseCat.None || phoneExact))
        {
            if (moji) cats |= CaseCat.Mojibake;
            if (arz) cats |= CaseCat.Arabizi;
        }
        return cats;
    }
}
