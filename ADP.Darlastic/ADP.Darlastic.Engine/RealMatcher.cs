namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Real-data matcher. Differs from the synthetic <see cref="Matcher"/> because profiling
/// (2026-06-07) showed the real signal landscape: phone is the king key (near-unique when valid),
/// name is fuzzy-but-collision-prone (mohammed x463), national ID is rare-but-strong,
/// DOB is dirty (placeholder + registration-date contamination) and must never penalize on weak evidence.
/// </summary>
public static class RealMatcher
{
    /// <summary>Confidence in [0,1] for a candidate pair.</summary>
    public static double Score(RealRecord a, RealRecord b) => ScoreCore(a, b, useAddress: true, useVin: true, out _, null);

    /// <summary>useAddress:false scores without the address signal — the diagnostic path that
    /// quantifies what address corroboration changed (`dotnet run real` prints the delta).</summary>
    public static double Score(RealRecord a, RealRecord b, bool useAddress) => ScoreCore(a, b, useAddress, useVin: true, out _, null);

    /// <summary>useAddress/useVin toggles — the ablation path that quantifies what each lever changed
    /// (`dotnet run real` / `eval` print the with-vs-without delta).</summary>
    public static double Score(RealRecord a, RealRecord b, bool useAddress, bool useVin) => ScoreCore(a, b, useAddress, useVin, out _, null);

    /// <summary>Score + which rules participated — the case browser's category source. Flags are
    /// bit-ors on the live path, cheap enough for the 12.4M-pair index walk.</summary>
    public static double Score(RealRecord a, RealRecord b, out MatchFlags flags) => ScoreCore(a, b, useAddress: true, useVin: true, out flags, null);

    /// <summary>Explain-mode: same scoring, plus a step-by-step trace for ONE pair on demand.</summary>
    public static double Explain(RealRecord a, RealRecord b, MatchTrace trace) => ScoreCore(a, b, useAddress: true, useVin: true, out _, trace);

    private static double ScoreCore(RealRecord a, RealRecord b, bool useAddress, bool useVin, out MatchFlags flags, MatchTrace? trace)
    {
        flags = MatchFlags.None;
        double score = 0, weight = 0;

        bool namesBoth = a.NormName.Length > 0 && b.NormName.Length > 0;
        double nameSim = 0;
        if (namesBoth)
        {
            flags |= MatchFlags.NamesBoth;
            double tg = Sim.Trigram(a.NormName, b.NormName), jw = Sim.JaroWinkler(a.NormName, b.NormName);
            nameSim = 0.5 * tg + 0.5 * jw;
            score += 0.45 * nameSim;
            weight += 0.45;
            trace?.Add("signal", "Name similarity",
                $"\"{a.NormName}\" ~ \"{b.NormName}\" · trigram {tg:F2} · Jaro-Winkler {jw:F2} → blend {nameSim:F2} (weight 0.45)");
        }
        else trace?.Add("signal", "Name similarity", a.NormName.Length == 0 && b.NormName.Length == 0
            ? "no usable name on either side — signal skipped"
            : $"no usable name on {(a.NormName.Length == 0 ? "side A" : "side B")} — signal skipped");

        bool phonesBoth = (a.Phones.Length + a.WeakPhones.Length) > 0 && (b.Phones.Length + b.WeakPhones.Length) > 0;
        bool phoneExact = false, phoneSuffix = false;
        if (phonesBoth)
        {
            flags |= MatchFlags.PhonesBoth;
            // Exact = identical strong (10-digit) OR identical weak (9-digit; the missing-digit +964 export form).
            // Review 2026-06-07: identical weak phones previously fell through to the 0.7 suffix
            // path, demoting true same-dealer duplicates from auto-merge into the steward band.
            phoneExact = a.Phones.Any(p => b.Phones.Contains(p)) || a.WeakPhones.Any(p => b.WeakPhones.Contains(p));
            if (!phoneExact)
            {
                // Suffix-8 bridge ONLY between a weak 9-digit and a strong 10-digit number.
                // Strong-vs-strong must never bridge: two real mobiles differing in the operator
                // digit (077 Asiacell / 078 Korek / 079 Zain) share their last 8 digits.
                phoneSuffix = a.WeakPhones.Any(w => b.Phones.Any(p => Suffix8(p) == Suffix8(w)))
                           || b.WeakPhones.Any(w => a.Phones.Any(p => Suffix8(p) == Suffix8(w)));
            }
            if (phoneExact) flags |= MatchFlags.PhoneExact;
            if (phoneSuffix) flags |= MatchFlags.PhoneSuffix;
            score += 0.45 * (phoneExact ? 1.0 : phoneSuffix ? 0.7 : 0.0);
            weight += 0.45;
            trace?.Add("signal", "Phone",
                phoneExact ? $"exact shared number ({SharedPhone(a, b)}) → 1.00 (weight 0.45)"
                : phoneSuffix ? "weak 9-digit ↔ strong 10-digit suffix-8 bridge → 0.70 (weight 0.45)"
                : "numbers on both sides but none shared → 0.00 (weight 0.45)");
        }
        else trace?.Add("signal", "Phone", "no number on one side — signal skipped");

        bool idsBoth = a.NationalId is not null && b.NationalId is not null;
        bool idEqual = idsBoth && a.NationalId == b.NationalId;
        if (idsBoth)
        {
            flags |= MatchFlags.IdsBoth;
            if (idEqual) flags |= MatchFlags.IdEqual;
            score += 0.10 * (idEqual ? 1 : 0);
            weight += 0.10;
            trace?.Add("signal", "National ID", idEqual ? "IDs equal → 1.00 (weight 0.10)" : "IDs present but different → 0.00 here; conflict penalty applies below (weight 0.10)");
        }

        // VIN — the corroborating key that breaks the name-similarity ceiling (build plan step 4).
        // Trust is per-EDGE, not one signal (domain expert, 2026-06-22): a shared SOLD VIN with a
        // consistent ownership period is close to a king key (point-of-sale entry binds VIN↔buyer);
        // a shared SERVICED VIN is corroboration only (a walk-in may be a relative/driver/new owner).
        // VIN NEVER penalizes — like address/DOB it is asymmetric evidence (one person can own several
        // cars; two records of the same person can list different VINs), so weight is added ONLY when a
        // VIN is actually shared. Disjoint periods with a sale between = ownership transfer = two people
        // (the P7 gate) → no positive signal. The decisive 0.91 floor lives below, after the gates.
        string? sharedVin = null;
        var vinClass = (useVin && !Flags.Baseline) ? ClassifyVin(a.VinLinks, b.VinLinks, out sharedVin) : VinClass.None;
        if ((a.VinLinks?.Length ?? 0) > 0 && (b.VinLinks?.Length ?? 0) > 0) flags |= MatchFlags.VinBoth;
        if (vinClass is VinClass.SoldOverlap or VinClass.Serviced)
        {
            double strength = vinClass == VinClass.SoldOverlap ? 1.0 : 0.6;
            score += 0.45 * strength;
            weight += 0.45;
            flags |= vinClass == VinClass.SoldOverlap ? MatchFlags.VinSoldOverlap : MatchFlags.VinServiced;
            trace?.Add("signal", "VIN",
                vinClass == VinClass.SoldOverlap
                    ? $"shared SOLD VIN ({sharedVin}), ownership periods consistent → 1.00 (weight 0.45) — strong ownership key"
                    : $"shared SERVICED VIN ({sharedVin}) → 0.60 (weight 0.45) — corroboration (walk-in, may be a relative/driver)");
        }
        else if (vinClass == VinClass.Transfer)
        {
            flags |= MatchFlags.VinTransfer;
            trace?.Add("signal", "VIN", $"shared VIN ({sharedVin}) but disjoint periods with a sale between — ownership transfer (two people, P7) → no merge signal");
        }
        else if ((flags & MatchFlags.VinBoth) != 0)
            trace?.Add("signal", "VIN", "both sides carry VINs but none shared — no signal (a person may own several cars; never a penalty)");

        // Source-asserted same-entity reference — an explicit FK from one record to the other (a
        // CRM ticket raised FOR a known DMS customer carries that customer's key). Weighted like a
        // king key so a name-blanked or phone-less record still scores; the decisive floor (with
        // its name gate — reference data entry can be wrong or stale) sits below with the other
        // floors, and the national-ID conflict veto still crushes it.
        bool sameAsLinked = !Flags.Baseline && SameAsLinked(a, b);
        if (sameAsLinked)
        {
            flags |= MatchFlags.SameAsRef;
            score += 0.45;
            weight += 0.45;
            trace?.Add("signal", "Same-entity reference",
                "one side explicitly references the other's profile key (source-asserted FK, e.g. ticket → DMS customer) → 1.00 (weight 0.45)");
        }

        if (weight == 0)
        {
            trace?.Add("decide", "No usable signal", "no comparable attribute on both sides → 0.00", 0);
            return 0;
        }
        double conf = score / weight;
        trace?.Add("base", "Weighted base", $"{score:F3} / {weight:F2} → {conf:F3}", conf);

        // Never auto-merge on name alone: one hyper-common two-token name appeared 81x at a single dealer. A perfect
        // name score with no corroborating signal must land mid-steward-queue, well clear of
        // the merge bands (review 2026-06-07: 0.85 parked common-name strangers at band 2).
        // A shared (positive) VIN counts as corroboration — name is no longer the ONLY signal — so
        // it lifts the damp the same way a phone/ID does (two records of one hyper-common name on one serviced car are the
        // corroborated case the damp exists to spare, not the common-name-stranger case it targets).
        bool vinPositive = vinClass is VinClass.SoldOverlap or VinClass.Serviced;
        if (namesBoth && !phonesBoth && !idsBoth && !vinPositive)
        {
            conf *= 0.70;
            flags |= MatchFlags.NameOnlyDamp;
            trace?.Add("gate", "Name-only damp", "name is the only signal — never auto-merge on name alone (hyper-common short names repeat 80+ times per dealer) → ×0.70", conf);
        }

        // Name-aware phone decisiveness (review 2026-06-08). Phone is the king key, but equal
        // 0.45/0.45 weighting let a fuzzy name VETO an exact phone — 177 true matches with a
        // shared phone + spelling/token drift ('Hazim Jabir' / 'Hazem Jaber', 'Salam' /
        // 'Salam Ahmed') sat at 0.80-0.90, just under auto-merge. Conversely a shared *family/
        // shop* phone with clearly different names ('Emad Nahir' / 'Ghanim Ahmed') must NOT merge.
        // So gate the exact-phone signal on name consistency in BOTH directions.
        bool nameConsistent = false, nameConflict = false;
        if (!Flags.Baseline && phoneExact && namesBoth)
        {
            // Gender is NOT used as a conflict signal (domain-expert audit, 2026-06-22): the dealer
            // Gender column is unreliable — verified same-person merges had a verified female name
            // recorded M at one dealer and F at another, and verified male names
            // recorded F. A gender veto here false-split real matches with an aligned given name, so
            // it is removed; Gender stays ingested for survivorship/display, not matching. Genuine
            // spouse/family-on-one-line cases are caught by the given names differing, not by gender.
            if (IsLikelyOrg(a.NormName) || IsLikelyOrg(b.NormName))
            {
                // A business line (a name carrying 'Company'/'Group'/'Stock'/'Show Room'/…) — a
                // company phone fronts a different transactor per visit (Goran Company / Goran
                // Rozhbian), so the same-given-name person-merge must NOT apply. Left at the weighted
                // base (lands near-miss/steward, not auto-merge) pending the org-entity track.
                flags |= MatchFlags.OrgLine;
                trace?.Add("gate", "Business / organization line",
                    "an org name (Company/Group/Stock/Parts/Show Room) shares this phone — a company line, not one person; person-merge floor withheld (identical org names still merge on the weighted base — a placeholder-entity filter is the real fix)", conf);
            }
            else if (NameConsistent(a.NormName, b.NormName))
            {
                nameConsistent = true;
                flags |= MatchFlags.NameConsistent;
                conf = Math.Max(conf, 0.93);
                trace?.Add("gate", "Phone decisive (names consistent)",
                    "every token of the shorter name aligns to the longer (spelling drift, not a different person) — a fuzzy name must not veto a perfect phone → floor 0.93", conf);
            }
            else if (GivenNamesMatch(a.NormName, b.NormName))
            {
                // Shared exact phone + the GIVEN (first) name is the same person's, allowing Arabic/
                // Kurdish romanization drift (Bewar/Dewar, Essa/Issa, Kebin/Tebin) — the rest may be
                // a different chain slice (Hawre Ahmed / Hawre Haji: given+father vs given+grandfather).
                // Team domain rule (2026-06-22): a shared phone + the same given name is ONE person
                // unless a hard ID conflict says otherwise. It does NOT fire when the GIVEN names
                // genuinely differ — siblings (sharing the father token, dissimilar given) and father/son name-order
                // (father/son name-order pairs) share a LATER token, not the given, so they stay in
                // the steward band. The phone match is the context that makes a *similar* given name
                // safe to treat as a spelling variant.
                flags |= MatchFlags.PhoneNameMerge;
                conf = Math.Max(conf, 0.91);
                trace?.Add("gate", "Phone decisive (same given name — chain slices)",
                    $"shared exact phone + same given name (spelling drift allowed), rest drifts (blend {nameSim:F2}) — one person across name-chain slices (team domain rule) → floor 0.91", conf);
            }
            else if (NameConflict(a.NormName, b.NormName, nameSim))
            {
                nameConflict = true;
                flags |= MatchFlags.NameConflictCap;
                conf = Math.Min(conf, 0.55);
                trace?.Add("gate", "Name conflict (no shared token, no similarity)",
                    $"name blend {nameSim:F2} < 0.30 and no shared token — completely different names → cap 0.55", conf);
            }
            else trace?.Add("gate", "Phone not decisive (given names differ)",
                $"shared phone but the given names don't align (blend {nameSim:F2}) — siblings / father-son name order / unrelated; stays in the steward band, no floor", conf);
        }

        // Address corroboration (2026-06-12; independently requested by a tenant as a match input).
        // Same posture as DOB: corroboration-only — a mismatch must NEVER penalize (the same
        // one verified customer carries two different governorates across dealers; people move and buy
        // across cities). Two gates keep it evidence, not population prior:
        //   1. BOTH sides must carry sub-city detail (NormAddress beyond the city slot) — when an
        //      address degenerates to just 'erbil', equality is city-membership in a 1.5M city,
        //      not corroboration (eval pair 501 caught exactly this).
        //   2. The 0.60 bar is DISTRICT-level similarity on the real corpus:
        //        'shahidan zargata sulaymaniyah' ~ 'shahidani zargata zaragata sulaymaniyah' = 0.83
        //        a misspelled same-district pair            = 0.65
        //        city-only overlap / same-city-different-street                              = 0.41-0.51
        // City itself stays an ingested + golden-surfaced attribute and participates inside the
        // full-address comparison; it is just never SUFFICIENT alone.
        // Sits BEFORE the ID/DOB conflict penalties so strong conflicts still crush it.
        if (!Flags.Baseline && useAddress && !nameConflict
            && a.NormAddress.Length > a.NormCity.Length && b.NormAddress.Length > b.NormCity.Length)
        {
            double addrSim = 0.5 * Sim.Trigram(a.NormAddress, b.NormAddress) + 0.5 * Sim.JaroWinkler(a.NormAddress, b.NormAddress);
            if (addrSim >= 0.60)
            {
                conf = Math.Min(1.0, conf + 0.05);
                flags |= MatchFlags.AddressBoost;
                trace?.Add("address", "Address corroboration",
                    $"\"{a.NormAddress}\" ~ \"{b.NormAddress}\" = {addrSim:F2} ≥ 0.60 (district-level agreement) → +0.05; a mismatch never penalizes", conf);
                // Chain-slice rescue → AUTO-MERGE (raised 0.88 → 0.91, 2026-06-12 domain-expert
                // audit). 'Hogr Jaafer'/'Hogr Jamil' on one phone at one address is ONE person
                // captured as different slices of the name chain (given+father vs given+
                // grandfather) — relatives who share a phone/house share the FATHER token, not
                // the GIVEN token. Expert calibration (team, name-chain domain): for this shape to be
                // two people you need cousins-named-after-one-grandfather in one compound (rare)
                // or a number re-sold to a same-given-name stranger (resale is itself the rare
                // different-people-one-phone path — the NameConflict guard's territory); a
                // steward would merge these on sight at ~100% confidence. Parking them in the
                // queue is worse than merging: obvious-to-human cases breed steward complacency,
                // and then REAL conflicts get rubber-stamped (queue-hygiene principle, Phase 5).
                // All five panel labels of this shape were 0/2/1 split votes escalated for human
                // audit — the audit flipped them (out/label_audits.csv).
                // Hard evidence still vetoes AFTER this boost: conflicting national IDs (×0.3)
                // and conflicting DOBs (×0.6) push a 0.91 well below the line. Without district-
                // level address agreement (city-only or no address) the pair stays steward-band;
                // extending the rule to phone+given-name-only is a pending expert call.
                //
                // The rescue demands MORE than the +0.05 boost: the addresses must still agree at
                // ≥ 0.60 after the city tokens and 'al' articles are stripped. Full-text blends
                // inflate when an address is mostly city ('al najaf najaf' ~ 'al zahraa najaf'
                // cleared 0.60 on repeated city tokens — corpus diagnostic 2026-06-12); district
                // evidence is what makes a same-given-name pair one person, so the rescue requires
                // the DISTRICT to match, not the city pattern.
                if (phoneExact && !nameConsistent && namesBoth && FirstTokensAlign(a.NormName, b.NormName))
                {
                    string subA = SubCityRemainder(a), subB = SubCityRemainder(b);
                    if (subA.Length >= 4 && subB.Length >= 4
                        && 0.5 * Sim.Trigram(subA, subB) + 0.5 * Sim.JaroWinkler(subA, subB) >= 0.60)
                    {
                        flags |= MatchFlags.ChainSliceRescue;
                        conf = Math.Max(conf, 0.91);
                        trace?.Add("address", "Chain-slice rescue",
                            $"same given name + exact phone + district remainder \"{subA}\" ~ \"{subB}\" agrees ≥ 0.60 — one person captured as different slices of the name chain; a steward would merge on sight (expert-audited rule, 2026-06-12) → 0.91 auto-merge", conf);
                    }
                }
            }
            else trace?.Add("address", "Address present, no agreement",
                $"\"{a.NormAddress}\" ~ \"{b.NormAddress}\" = {addrSim:F2} < 0.60 — below district-level agreement; no penalty (people move and buy across cities)", conf);
        }
        else if (trace is not null && useAddress && !nameConflict)
        {
            trace.Add("address", "Address signal skipped",
                a.NormAddress.Length == 0 && b.NormAddress.Length == 0 ? "no address on either side"
                : a.NormAddress.Length == 0 || b.NormAddress.Length == 0 ? "no address on one side"
                : "address is city-only on at least one side — equality would be city-membership in a 1.5M city, not corroboration", conf);
        }

        // Shared SOLD VIN → 0.91 auto-merge (build plan step 4): a point-of-sale VIN↔buyer binding is
        // close to a king key, so let it cross the line with even a weak name — the way an exact phone
        // does for an aligned given name. Guarded BOTH ways: a hard name conflict (different transactor
        // on the bill of sale — a relative/driver, or resale to a stranger) holds it to the weighted
        // base (steward), and the national-ID conflict veto below still crushes it (×0.3). Sits before
        // the conflict penalties for exactly that reason. Service-grade VINs never floor — they only
        // corroborate via the weighted contribution above.
        if (useVin && !Flags.Baseline && vinClass == VinClass.SoldOverlap)
        {
            // Mirror the phone+given rule exactly: a sold VIN is decisive when the GIVEN names align
            // (spelling drift allowed) OR there is no usable name to contradict it (VIN-only). Genuinely
            // different given names on one sold car = a different transactor (father/son, resale, a driver)
            // → keep it in the steward band on the weighted contribution, do NOT auto-merge. Evidence: the
            // looser !NameConflict guard (block only when name-sim < 0.30) added exactly 1 gold-set false
            // merge ('ala talab' ~ 'wathq talb' — one sold car, different people) and 0 recall wins, since
            // same-person VIN pairs already merge on phone/name. The national-ID conflict veto still applies.
            // Auto-merge on a sold VIN ONLY when names are FULLY consistent (every token of the shorter
            // aligns — spelling drift / chain slice) or there is no name to contradict it. A merely-aligned
            // GIVEN name is NOT enough: a car is shared by relatives / resale far more than a phone is, and
            // GivenNamesMatch's JW≥0.80 admits genuinely different names that share a prefix. vin-sample
            // (2026-06-22) made it concrete — the given-only bucket held clear false merges on one sold car
            // (gold-confirmed different-people pairs sharing a sold VIN). So given-only
            // keeps only the weighted sold contribution (lands near-miss / steward) and is flagged, but does
            // NOT auto-merge. Chain-slice (12 gold 'same', 0 'different') stays 0.91. Loosening back to
            // given-only is a pending expert call.
            if (!namesBoth || NameConsistent(a.NormName, b.NormName))
            {
                conf = Math.Max(conf, 0.91);
                flags |= MatchFlags.VinSoldMerge;
                trace?.Add("vin", "Shared sold VIN (ownership consistent, names align)",
                    $"a point-of-sale VIN↔buyer binding ({sharedVin}) + a fully-aligned/absent name — king-key-grade ownership → floor 0.91 auto-merge", conf);
            }
            else if (namesBoth && GivenNamesMatch(a.NormName, b.NormName))
                trace?.Add("vin", "Shared sold VIN, given name aligns but a later token differs",
                    $"shared sold VIN ({sharedVin}) + same given name but the rest differs — a car is shared by relatives / resale; steward-band on the weighted signal, no auto-merge (vin-sample 2026-06-22)", conf);
            else trace?.Add("vin", "Shared sold VIN but names differ",
                $"shared sold VIN ({sharedVin}) but the names point to different people — corroboration only, no auto-merge floor", conf);
        }

        // Same-entity reference → auto-merge floor, name-gated exactly like the sold-VIN rule: the
        // FK is decisive when the names are fully consistent OR there is no usable name to
        // contradict it (a validated-fake lead name is blanked at ingest, leaving the FK + phone to
        // carry the identity). An aligned GIVEN name still floors — the reference context makes
        // given-name drift safe the way a shared phone does — but genuinely different names hold
        // the pair at the weighted base (steward band): a ticket can be raised on the wrong
        // customer record, and a stale FK after a number/name change looks exactly like that.
        if (sameAsLinked)
        {
            if (!namesBoth || NameConsistent(a.NormName, b.NormName) || GivenNamesMatch(a.NormName, b.NormName))
            {
                conf = Math.Max(conf, namesBoth && NameConsistent(a.NormName, b.NormName) ? 0.95 : 0.91);
                flags |= MatchFlags.SameAsMerge;
                trace?.Add("gate", "Same-entity reference decisive",
                    "explicit FK + names consistent/aligned (or no name to contradict) → auto-merge floor", conf);
            }
            else trace?.Add("gate", "Same-entity reference held back",
                "explicit FK but the names point to different people — possibly a ticket raised on the wrong customer record; steward band on the weighted base", conf);
        }

        // Conflict penalties — only on strong, plausibility-filtered evidence.
        if (idsBoth && !idEqual)
        {
            conf *= 0.3;
            flags |= MatchFlags.IdConflict;
            trace?.Add("conflict", "National-ID conflict", "different national IDs — hard evidence of two people → ×0.3", conf);
        }
        if (a.Dob is not null && b.Dob is not null)
        {
            if (a.Dob.EqualsLoose(b.Dob))
            {
                conf = Math.Min(1.0, conf + 0.05);
                flags |= MatchFlags.DobEqual;
                trace?.Add("conflict", "DOB corroboration", $"same date under a consistent day/month reading ({a.Dob.P1}/{a.Dob.P2}/{a.Dob.Year}) → +0.05", conf);
            }
            else if (a.Dob.ConflictsWith(b.Dob))
            {
                // DOB is corroboration-ONLY (domain-expert ruling, 2026-06-22): the dealer DOB field
                // is unreliable enough that a conflict is noise, not evidence of two people — a
                // verified self-example had one dealer blank, another '1999', true value 1992. So a
                // conflicting DOB no longer penalizes (it used to ×0.6 and was splitting true
                // matches the phone+given rule should keep together); the flag stays for steward
                // visibility only. National-ID conflict (a reliable key) remains the hard veto above.
                flags |= MatchFlags.DobConflict;
                trace?.Add("conflict", "DOB differs (noted, not penalized)",
                    $"{a.Dob.P1}/{a.Dob.P2}/{a.Dob.Year} vs {b.Dob.P1}/{b.Dob.P2}/{b.Dob.Year} — dealer DOB is unreliable; flagged for the steward but no score penalty", conf);
            }
        }

        trace?.Add("decide", BandTitle(conf), $"final confidence {conf:F3} → {BandLabel(Band(conf))}", conf);
        return conf;
    }

    private static string BandTitle(double conf) =>
        conf >= 0.90 ? "Auto-merge" : conf >= 0.80 ? "Steward queue" : conf >= 0.50 ? "Tracked, not merged" : "Ignored";

    private static string SharedPhone(RealRecord a, RealRecord b) =>
        a.Phones.FirstOrDefault(p => b.Phones.Contains(p))
        ?? a.WeakPhones.FirstOrDefault(p => b.WeakPhones.Contains(p)) ?? "?";

    // Same person under spelling drift vs. two relatives on one phone is decided PER TOKEN, not by
    // an overall blend: every token of the shorter name must align to a token of the longer one.
    // 'Hazim Jabir'/'Hazem Jaber' aligns (both tokens drift); 'Hogr Jafar'/'Hogr Jamil' does NOT
    // (first identical, second a different word) — that's a sibling pair, left to the steward band.
    public static bool NameConsistent(string a, string b)
    {
        var ta = NameTokens(a);
        var tb = NameTokens(b);
        if (ta.Length == 0 || tb.Length == 0) return false;
        var (small, big) = ta.Length <= tb.Length ? (ta, tb) : (tb, ta);
        // A single distinct token — a true mononym ('Salam') or a repeated junk name
        // ('Mohammed Mohammed') — requires that many EXACT occurrences in the longer name. So
        // 'Salam' still matches 'Salam Ahmed', but 'Mohammed Mohammed' does NOT match 'Mohammed
        // Thany' (only one 'mohammed'), which was the one residual false-merge (review 2026-06-08).
        if (small.Distinct().Count() < 2)
        {
            string tok = small[0];
            return big.Count(t => t == tok) >= small.Length;
        }
        return small.All(t => big.Any(u => TokensAlign(t, u)));
    }

    // The address minus its own city tokens and 'al' articles — the district/street part that
    // carries real corroborating power. '' when the address is only city ('al najaf najaf' → '').
    // Fused articles are stripped too ('alameer'→'ameer'): district names overwhelmingly start
    // with al-, which inflates trigram/JW enough that DIFFERENT districts cleared the 0.60 bar
    // ('alameer' ~ 'algadeer' = 0.62 fused, 0.40 stripped — corpus diagnostic 2026-06-12).
    private static string SubCityRemainder(RealRecord r)
    {
        var cityToks = r.NormCity.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rest = r.NormAddress.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Where(t => t != "al" && !cityToks.Contains(t))
                                .Select(t => t.Length >= 5 && t.StartsWith("al") ? t[2..] : t);
        return string.Join(' ', rest);
    }

    // First tokens are each side's GIVEN name in the Arabic name-chain (given + father + grandfather):
    // chain slices of one person share it; relatives on one phone (siblings, father/son) differ on it.
    // Internal: the case-browser index re-applies it to tag the phone+given increment shape.
    public static bool FirstTokensAlign(string a, string b)
    {
        var ta = NameTokens(a);
        var tb = NameTokens(b);
        return ta.Length > 0 && tb.Length > 0 && TokensAlign(ta[0], tb[0]);
    }

    // The given (first) names belong to the same person, allowing Arabic/Kurdish romanization drift
    // (Bewar/Dewar, Essa/Issa, Kebin/Tebin, Mohtaz/Muhtaj). Looser than TokensAlign's ~0.90 bar
    // because the caller only invokes it on a SHARED EXACT PHONE — that context makes a merely-
    // similar given name overwhelmingly a spelling variant, not two people. Siblings / father-son
    // have dissimilar given names (sibling given names are typically dissimilar, JW≈0.3) and stay below the 0.80 bar.
    // BUT raw JW≥0.80 also admits DIFFERENT names that share a prefix/shape — Arshad/Ahmad JW=0.84
    // (steward-flagged false merge 2026-06-24: a family sharing a phone, not one person). The consonant-
    // skeleton guard separates them: spelling drift keeps (near-)identical skeletons (Bewar/Dewar br/dr=1,
    // Hameed/Hamed hmd/hmd=0, Essa/Issa s/s=0) while Arshad/Ahmad is rshd/hmd=3. So the JW path also
    // requires skeleton edit-distance ≤ 1. (Salam/Salah slm/slh=1 stays a separate open item.)
    public static bool GivenNamesMatch(string a, string b)
    {
        var ta = NameTokens(a);
        var tb = NameTokens(b);
        if (ta.Length == 0 || tb.Length == 0) return false;
        return TokensAlign(ta[0], tb[0])
            || (Sim.JaroWinkler(ta[0], tb[0]) >= 0.80 && SkeletonDistance(ta[0], tb[0]) <= 1);
    }

    // Consonant-skeleton edit distance — the discriminator that lets GivenNamesMatch keep romanization
    // drift but reject different names that merely share a JW prefix. Folds the interdental class first
    // (Canon) then drops vowels/glides/doubles (Skeleton), exactly as TokensAlign does.
    private static int SkeletonDistance(string a, string b) => Levenshtein(Skeleton(Canon(a)), Skeleton(Canon(b)));

    private static int Levenshtein(string a, string b)
    {
        var d = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) d[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            int prev = d[0]; d[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cur = d[j];
                d[j] = Math.Min(Math.Min(d[j] + 1, d[j - 1] + 1), prev + (a[i - 1] == b[j - 1] ? 0 : 1));
                prev = cur;
            }
        }
        return d[b.Length];
    }

    // Tokenize for name comparison, dropping a standalone Arabic article 'al' — it's a dangling
    // fragment of a split 'Al-Something' surname ('Something Al' ⟶ 'something'),
    // not a name token. Keep the raw split if stripping would empty the name.
    public static string[] NameTokens(string s)
    {
        var raw = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stripped = raw.Where(t => t != "al").ToArray();
        return stripped.Length > 0 ? stripped : raw;
    }

    // Shared fuzzy-token primitive — also drives Merge's longest-chain survivorship.
    public static bool TokensAlign(string t, string u)
    {
        if (t == u) return true;
        if (t.Length >= 4 && u.Length >= 4 && (u.Contains(t) || t.Contains(u))) return true;
        double jw = Sim.JaroWinkler(t, u);
        if (jw >= 0.90) return true;
        // Arabic transliteration varies mostly in short vowels and doubled consonants
        // (Hameed/Hamed, Zaidan/Zidan, adil/adel, Husen/Hussain). Same consonant skeleton + a
        // near-miss JW = same token; 'jafar'/'jamil' (jfr/jml) and skeleton collisions like
        // 'hemn'/'haiman' (both hmn, but JW 0.64) stay distinct.
        // Fold the interdental/emphatic class (Arabic ظ ذ ض) FIRST: one letter romanizes as
        // z / dh / th / zh across dealers — Thahir/Zahir, Kadhim/Kazem, Mudhaffar/
        // Muzaffar (مظفر). Comparing the folded forms lets BOTH the consonant skeleton and the
        // near-miss JW gate treat the variants as one letter (raw JW(thahir,zahir)=0.74 would
        // otherwise veto the skeleton match). Canon is a strict no-op on any token without those
        // digraphs, so non-ظ tokens — incl. the hemn/haiman and jafar/jamil negatives above —
        // skeleton, gate, and score exactly as before.
        string ct = Canon(t), cu = Canon(u);
        bool folded = !ReferenceEquals(ct, t) || !ReferenceEquals(cu, u);
        double cjw = folded ? Sim.JaroWinkler(ct, cu) : jw;
        string st = Skeleton(ct), su = Skeleton(cu);
        if (st == su) return st.Length >= 2 && cjw >= 0.80;
        // Glottal-h drift: a weak Arabic ع/ء/ه/ح is written 'h' on one side and as a vowel/nothing
        // on the other, so the consonant skeletons differ by exactly one 'h' — Ashad/Asaad (أسعد),
        // Rafih/Rafa, Maghdid/Magded. Treat as one token when the folded forms are already a
        // near-match (cjw ≥ 0.80). Narrow by construction — ONLY an 'h' indel: Karim/Kazim is a
        // z↔r substitution (unchanged), Salam/Salman is an 'n' indel (unchanged), Jafar/Jamil
        // differs in two letters (unchanged) — so siblings and the Splink skeleton-collision
        // negatives (hemn/haiman) are untouched. Gold-set gated like every prior skeleton fix.
        return cjw >= 0.80 && DiffersByOneH(st, su);
    }

    // True when two skeletons differ by exactly one inserted/deleted 'h' and nothing else: the
    // longer equals the shorter with a single 'h' spliced in. Shorter must be ≥ 2 so a bare
    // 'h'+letter can't bridge two near-empty skeletons.
    private static bool DiffersByOneH(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) != 1) return false;
        var (s, l) = a.Length < b.Length ? (a, b) : (b, a);
        if (s.Length < 2) return false;
        for (int i = 0; i < l.Length; i++)
            if (l[i] == 'h' && l.Remove(i, 1) == s) return true;
        return false;
    }

    // Fold the z/dh/th/zh interdental-emphatic transliteration class (Arabic ظ ذ ض) to a single
    // 'z' so spelling variants of one Arabic letter compare equal. Returns the SAME instance when
    // no digraph is present (the common case), which lets TokensAlign skip recomputing similarity
    // and guarantees byte-identical behavior for every token the fold doesn't touch. Used only in
    // TokensAlign's fuzzy comparison — it never rewrites a stored or displayed name. 'th' here is
    // the ظ/ذ romanization; a genuine 'th'=ث name (Othman, Haitham) only collides if every OTHER
    // letter already matches, which the gold-set + corpus face-validity gates check.
    private static string Canon(string t) =>
        t.Contains("dh") || t.Contains("th") || t.Contains("zh")
            ? t.Replace("dh", "z").Replace("th", "z").Replace("zh", "z")
            : t;

    private static string Skeleton(string t)
    {
        var sb = new System.Text.StringBuilder(t.Length);
        char last = '\0';      // last APPENDED consonant — for the doubled-consonant collapse
        char prevRaw = '\0';   // previous INPUT char — for aw/au glide detection
        foreach (char c in t)
        {
            // 'y' counts as a vowel: it's the variable final/medial vowel in Arabic transliteration
            // (khaelany/khailani, -aty/-ati), so dropping it lets those variants share a skeleton.
            bool vowel = c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
            // 'w' AFTER a vowel is the glide of an aw/au/ow diphthong — the same sound as the 'u' it
            // alternates with across transliterations (Maulud/Mawlud, Daud/Dawud), so drop it like a
            // vowel: 'mawlud'→'mld' now matches 'maulud'→'mld'. A 'w' word-initial or after a
            // consonant is a real consonant (Anwar→'nwr', Marwan→'mrwn', Salwa→'slw') and is kept —
            // the glide is gated on a preceding vowel so genuine-consonant 'w' is untouched.
            bool glideW = c == 'w' && prevRaw is 'a' or 'e' or 'i' or 'o' or 'u';
            prevRaw = c;
            if (vowel || glideW) continue;
            if (c == last) continue; // collapse doubled consonants
            sb.Append(c);
            last = c;
        }
        return sb.ToString();
    }

    // Likely a shared family/shop phone across two different people: low similarity AND no shared token.
    private static bool NameConflict(string a, string b, double nameSim) =>
        nameSim < 0.30 && !SharesToken(a, b);

    private static bool SharesToken(string a, string b)
    {
        var tb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return a.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(tb.Contains);
    }

    // Tokens that mark a name as a business/organization line rather than a person — a company
    // phone fronts a different transactor per visit, so these must not person-merge. Conservative
    // list of forms seen in the dealer data (post-transliteration, lowercased); the dealer-self
    // records (a dealer's own name as a customer; 'parts stock') still need a separate name-list filter.
    private static readonly HashSet<string> OrgTokens =
        ["company", "group", "grop", "stock", "motors", "trading", "showroom", "factory", "sharika", "parts"];
    private static bool IsLikelyOrg(string normName) =>
        normName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(OrgTokens.Contains);

    private static string Suffix8(string phone) => phone.Length <= 8 ? phone : phone[^8..];

    // VIN-share classification + the P7 ownership-period gate (build plan step 4). Strength order
    // SoldOverlap > Serviced > Transfer > None; the strongest shared VIN decides. Arrays are tiny (a
    // profile holds few cars) and this runs only when both sides carry VINs, so the O(n·m) scan is cheap.
    private enum VinClass { None = 0, Transfer = 1, Serviced = 2, SoldOverlap = 3 }

    private static VinClass ClassifyVin(VinLink[]? av, VinLink[]? bv, out string? sharedVin)
    {
        sharedVin = null;
        if (av is null || bv is null || av.Length == 0 || bv.Length == 0) return VinClass.None;
        VinClass best = VinClass.None;
        for (int i = 0; i < av.Length; i++)
        {
            string vin = av[i].Vin;
            bool dup = false;
            for (int k = 0; k < i; k++) if (av[k].Vin == vin) { dup = true; break; }   // evaluate each shared VIN once
            if (dup) continue;
            bool bHas = false;
            for (int j = 0; j < bv.Length; j++) if (bv[j].Vin == vin) { bHas = true; break; }
            if (!bHas) continue;
            var c = ClassifyOneVin(av, bv, vin);
            if (c > best) { best = c; sharedVin = vin; if (best == VinClass.SoldOverlap) break; }
        }
        return best;
    }

    private static VinClass ClassifyOneVin(VinLink[] av, VinLink[] bv, string vin)
    {
        var (aSale, aSaleFirst, _, aMax) = ScanVinSide(av, vin);
        var (bSale, bSaleFirst, _, bMax) = ScanVinSide(bv, vin);
        if (!aSale && !bSale) return VinClass.Serviced;          // both sides walk-in service only
        if (aSale && bSale)
        {
            // Two purchase events on one VIN: the same sale recorded at both dealers (≈ same date) is one
            // ownership; purchases far apart are a resale → ownership transfer (different buyers).
            if (aSaleFirst.HasValue && bSaleFirst.HasValue
                && Math.Abs(aSaleFirst.Value.DayNumber - bSaleFirst.Value.DayNumber) > 31) return VinClass.Transfer;
            return VinClass.SoldOverlap;
        }
        // Exactly one side is the seller-of-record; the other only serviced the car. If that other side's
        // activity is entirely BEFORE the sale, it was the previous owner → transfer; otherwise same owner.
        DateOnly? saleDate = aSale ? aSaleFirst : bSaleFirst;
        DateOnly? otherMax = aSale ? bMax : aMax;
        if (saleDate.HasValue && otherMax.HasValue && otherMax.Value < saleDate.Value) return VinClass.Transfer;
        return VinClass.SoldOverlap;
    }

    private static (bool Sale, DateOnly? SaleFirst, DateOnly? Min, DateOnly? Max) ScanVinSide(VinLink[] links, string vin)
    {
        bool sale = false; DateOnly? saleFirst = null, min = null, max = null;
        foreach (var l in links)
        {
            if (l.Vin != vin) continue;
            DateOnly? lo = l.First ?? l.Last, hi = l.Last ?? l.First;
            if (lo.HasValue && (min is null || lo < min)) min = lo;
            if (hi.HasValue && (max is null || hi > max)) max = hi;
            if (l.Source == VinSource.Sale)
            {
                sale = true;
                if (lo.HasValue && (saleFirst is null || lo < saleFirst)) saleFirst = lo;
            }
        }
        return (sale, saleFirst, min, max);
    }

    /// <summary>
    /// Blocking for 716K records. Phone blocks are the workhorse; name blocks catch
    /// same-person-different-number but explode on common names, so they are size-capped
    /// WITH explicit reporting (no silent truncation).
    /// </summary>
    public static BlockingResult BuildBlocks(IReadOnlyList<RealRecord> records, int phoneBlockCap = 50, int nameBlockCap = 200, bool keepKeyMap = false)
    {
        var blocks = new Dictionary<string, List<int>>();
        void Add(string key, int idx)
        {
            if (!blocks.TryGetValue(key, out var list)) blocks[key] = list = [];
            list.Add(idx);
        }

        foreach (var r in records)
            foreach (var key in BlockKeysOf(r))
                Add(key, r.Idx);

        var result = new BlockingResult();
        foreach (var (key, list) in blocks)
        {
            // VIN blocks are capped like phone blocks (build plan step 3): a fleet / dealer-plate VIN
            // must not explode the candidate set. Tracked + reported separately, never silently truncated.
            bool isVin = key[0] == 'V';
            if (isVin && list.Count > result.MaxVinBlock) result.MaxVinBlock = list.Count;
            bool capped = key[0] is 'P' or 'S' or 'I' or 'V';
            int cap = capped ? phoneBlockCap : nameBlockCap;
            if (list.Count > cap)
            {
                result.SkippedBlocks++;
                result.SkippedBlockRecords += list.Count;
                if (key[0] is 'P' or 'S' or 'I') result.SkippedPhoneBlocks++; // placeholder numbers like '...0000' (54 rows)
                if (isVin) result.SkippedVinBlocks++;                          // fleet / dealer-plate VINs
                if (keepKeyMap) result.CappedKeys?.Add(key, list.Count);
                continue;
            }
            if (list.Count > 1)
            {
                result.Blocks.Add(list);
                if (keepKeyMap) result.KeyBlocks?.Add(key, list);
            }
        }
        if (!keepKeyMap) { result.KeyBlocks = null; result.CappedKeys = null; }
        return result;
    }

    /// <summary>
    /// The block-key recipe for one record — the single source BuildBlocks consumes, exposed so
    /// the case browser can show WHY a pair met (shared keys) and find a record's candidates
    /// without rebuilding blocking. "S:" suffix keys exist solely to co-block weak 9-digit phones
    /// with their strong 10-digit counterparts; strong-strong pairs that meet only there score 0
    /// on the phone signal (Score gates the suffix bridge on a weak side), so the extra pairs are
    /// harmless.
    /// </summary>
    public static IEnumerable<string> BlockKeysOf(RealRecord r)
    {
        // Dealer-self / placeholder org (Parts Stock, the dealer's own name) is not a customer — emit NO
        // block keys so it never pairs with anything and stays a singleton. Blanking the name wouldn't
        // suffice: these share the dealer's phone and would still merge on it.
        if (r.IsOrgPlaceholder) yield break;
        foreach (var p in r.Phones) yield return "P:" + p;
        foreach (var p in r.Phones) yield return "S:" + (p.Length <= 8 ? p : p[^8..]);
        foreach (var p in r.WeakPhones) yield return "S:" + (p.Length <= 8 ? p : p[^8..]);
        if (r.NationalId is not null) yield return "I:" + r.NationalId;
        // VIN co-blocks every profile that bought/serviced the same car — the new cross-dealer bridge
        // (build plan step 3). Distinct VINs only (a profile may hold one VIN as both sale+service).
        if (r.VinLinks is { Length: > 0 })
        {
            var seenVin = new HashSet<string>();
            foreach (var l in r.VinLinks) if (seenVin.Add(l.Vin)) yield return "V:" + l.Vin;
        }
        if (r.NormName.Length >= 6) yield return "N:" + r.NormName[..6];
        var tokens = r.NormName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
            yield return "T:" + tokens[0][..Math.Min(4, tokens[0].Length)] + "+" + tokens[^1][..Math.Min(4, tokens[^1].Length)];
        // Same-entity references co-block the referrer with the record it points at: every record
        // publishes its OWN key, ref-carriers additionally publish each target key. Non-referenced
        // records form singleton R-blocks, which pair-generation skips for free.
        yield return "R:" + r.SourceSystem + "|" + r.SourceRecordId;
        if (r.SameAsRefs is { Length: > 0 })
            foreach (var refKey in r.SameAsRefs) yield return "R:" + refKey;
    }

    /// <summary>True when either side carries a source-asserted reference to the other's profile key.</summary>
    public static bool SameAsLinked(RealRecord a, RealRecord b) =>
        (a.SameAsRefs is { Length: > 0 } ra && ra.Contains(b.SourceSystem + "|" + b.SourceRecordId))
        || (b.SameAsRefs is { Length: > 0 } rb && rb.Contains(a.SourceSystem + "|" + a.SourceRecordId));

    public sealed class BlockingResult
    {
        public List<List<int>> Blocks = [];
        public int SkippedBlocks, SkippedPhoneBlocks, SkippedVinBlocks, MaxVinBlock;
        public long SkippedBlockRecords;
        /// <summary>key → members, uncapped blocks only; populated when BuildBlocks(keepKeyMap: true) (case browser).</summary>
        public Dictionary<string, List<int>>? KeyBlocks = new();
        /// <summary>key → would-be size for capped (skipped) keys; the browser explains "this key was capped".</summary>
        public Dictionary<string, int>? CappedKeys = new();
    }

    /// <summary>
    /// Stream-score all blocked pairs: keeps per-band counts + a deterministic reservoir sample
    /// per band (for the labeling panel), never materializing the full pair list.
    /// </summary>
    public static ScoringResult ScorePairs(IReadOnlyList<RealRecord> records, BlockingResult blocking, int reservoirPerBand = 1500, int seed = 42)
    {
        var rng = new Random(seed);
        var result = new ScoringResult();
        var seen = new HashSet<long>();

        foreach (var block in blocking.Blocks)
        {
            for (int i = 0; i < block.Count; i++)
                for (int j = i + 1; j < block.Count; j++)
                {
                    int a = Math.Min(block[i], block[j]), b = Math.Max(block[i], block[j]);
                    if (a == b || !seen.Add(((long)a << 32) | (uint)b)) continue;

                    double s = Score(records[a], records[b]);
                    int band = Band(s);
                    result.BandCounts[band]++;

                    // Reservoir sampling per band (deterministic via fixed seed).
                    var res = result.BandSamples[band];
                    if (res.Count < reservoirPerBand) res.Add((a, b, s));
                    else
                    {
                        long n = result.BandCounts[band];
                        long k = NextLong(rng, n);
                        if (k < reservoirPerBand) res[(int)k] = (a, b, s);
                    }
                }
        }
        result.TotalPairs = result.BandCounts.Sum();
        return result;
    }

    private static long NextLong(Random rng, long max) => (long)(rng.NextDouble() * max);

    public static readonly double[] BandEdges = [0.95, 0.90, 0.80, 0.70, 0.60, 0.50, 0.0];
    public static string BandLabel(int band) => band switch
    {
        0 => ">=0.95", 1 => "0.90-0.95", 2 => "0.80-0.90", 3 => "0.70-0.80",
        4 => "0.60-0.70", 5 => "0.50-0.60", _ => "<0.50",
    };
    public static int Band(double s) => s >= 0.95 ? 0 : s >= 0.90 ? 1 : s >= 0.80 ? 2 : s >= 0.70 ? 3 : s >= 0.60 ? 4 : s >= 0.50 ? 5 : 6;

    public sealed class ScoringResult
    {
        public long[] BandCounts = new long[7];
        public List<(int A, int B, double Score)>[] BandSamples = [.. Enumerable.Range(0, 7).Select(_ => new List<(int, int, double)>())];
        public long TotalPairs;
    }
}

/// <summary>CSV emit for the Postgres load (long format) and the labeling sample.</summary>
public static class RealEmit
{
    public static (long Profiles, long Attributes) WriteAttributes(IReadOnlyList<RealRecord> records, string path)
    {
        long attrs = 0, profiles = 0;
        using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(false));
        w.WriteLine("source_system,source_record_id,attr_type,value,value_normalized");
        foreach (var r in records)
        {
            long before = attrs;
            if (r.RawName.Length > 0)
            { w.WriteLine(Line(r, "full_name", r.RawName, r.NormName.Length > 0 ? r.NormName : null)); attrs++; }
            foreach (var p in r.Phones) { w.WriteLine(Line(r, "phone", p, p)); attrs++; }
            foreach (var p in r.WeakPhones) { w.WriteLine(Line(r, "phone_partial", p, p)); attrs++; }
            if (r.NationalId is not null) { w.WriteLine(Line(r, "national_id", r.NationalId, r.NationalId)); attrs++; }
            if (r.Dob is not null)
            { w.WriteLine(Line(r, "dob", $"{r.Dob.P1}/{r.Dob.P2}/{r.Dob.Year}", $"{r.Dob.Year:D4}-{Math.Min(r.Dob.P1, r.Dob.P2):D2}-{Math.Max(r.Dob.P1, r.Dob.P2):D2}")); attrs++; }
            // Address (2026-06-12): full text for fuzzy probes/steward view + the city/governorate
            // slot separately (a tenant asked for city as a match input; spelling drifts — compare fuzzily).
            if (r.NormAddress.Length > 0)
            { w.WriteLine(Line(r, "address", r.RawAddress.Replace('|', ' '), r.NormAddress)); attrs++; }
            if (r.NormCity.Length > 0)
            { w.WriteLine(Line(r, "city", r.NormCity, r.NormCity)); attrs++; }
            if (attrs > before) profiles++; // a record with no usable attribute emits nothing and never reaches source_profile
        }
        return (profiles, attrs);

        static string Line(RealRecord r, string type, string value, string? norm) =>
            $"{r.SourceSystem},{r.SourceRecordId},{type},{Q(value)},{(norm is null ? "" : Q(norm))}";
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
    }

    /// <summary>Band-stratified candidate-pair sample for the labeling panel (real PII — stays local, gitignored).</summary>
    public static int WritePairSample(IReadOnlyList<RealRecord> records, RealMatcher.ScoringResult scored, string path, int perBand = 150, int seed = 7)
    {
        var rng = new Random(seed);
        int written = 0;
        using var w = new StreamWriter(path, false, new System.Text.UTF8Encoding(false));
        // addr_a/addr_b appended at the END so the pre-address column indices stay stable
        // (Freeze and the existing labeled_pairs.csv reference columns 0–14 by position).
        w.WriteLine("pair_id,band,score,src_a,id_a,name_a,phones_a,natid_a,dob_a,src_b,id_b,name_b,phones_b,natid_b,dob_b,addr_a,addr_b");
        for (int band = 0; band < 7; band++)
        {
            var sample = scored.BandSamples[band].OrderBy(_ => rng.Next()).Take(perBand);
            foreach (var (ai, bi, s) in sample)
            {
                var a = records[ai]; var b = records[bi];
                w.WriteLine($"{written},{RealMatcher.BandLabel(band)},{s:F3},{Cells(a)},{Cells(b)},{Q(a.RawAddress)},{Q(b.RawAddress)}");
                written++;
            }
        }
        return written;

        static string Cells(RealRecord r) =>
            $"{r.SourceSystem},{r.SourceRecordId},{Q(r.RawName)},{Q(string.Join(' ', r.Phones.Concat(r.WeakPhones.Select(p => p + "?"))))},{r.NationalId},{(r.Dob is null ? "" : $"{r.Dob.P1}/{r.Dob.P2}/{r.Dob.Year}")}";
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
    }
}
