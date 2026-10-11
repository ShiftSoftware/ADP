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
    public static double Score(RealRecord a, RealRecord b) => ScoreCore(a, b, useAddress: true, useVin: true, useEmail: true, out _, null);

    /// <summary>useAddress:false scores without the address signal — the diagnostic path that
    /// quantifies what address corroboration changed (`dotnet run real` prints the delta).</summary>
    public static double Score(RealRecord a, RealRecord b, bool useAddress) => ScoreCore(a, b, useAddress, useVin: true, useEmail: true, out _, null);

    /// <summary>useAddress/useVin toggles — the ablation path that quantifies what each lever changed
    /// (`dotnet run real` / `eval` print the with-vs-without delta).</summary>
    public static double Score(RealRecord a, RealRecord b, bool useAddress, bool useVin) => ScoreCore(a, b, useAddress, useVin, useEmail: true, out _, null);

    /// <summary>Full ablation triple. useEmail:false measures what e-mail changed on a tenant that has
    /// it enabled; on a tenant with <see cref="Flags.EmailMatching"/> off the signal is inert either way.</summary>
    public static double Score(RealRecord a, RealRecord b, bool useAddress, bool useVin, bool useEmail) => ScoreCore(a, b, useAddress, useVin, useEmail, out _, null);

    /// <summary>Score + which rules participated — the case browser's category source. Flags are
    /// bit-ors on the live path, cheap enough for the 12.4M-pair index walk.</summary>
    public static double Score(RealRecord a, RealRecord b, out MatchFlags flags) => ScoreCore(a, b, useAddress: true, useVin: true, useEmail: true, out flags, null);

    /// <summary>Explain-mode: same scoring, plus a step-by-step trace for ONE pair on demand.</summary>
    public static double Explain(RealRecord a, RealRecord b, MatchTrace trace) => ScoreCore(a, b, useAddress: true, useVin: true, useEmail: true, out _, trace);

    /// <summary>Score + flags under <paramref name="settings"/> — for example the configuration a resolve
    /// recorded — whatever the process-wide <see cref="Flags"/> say. Nothing process-wide changes.</summary>
    public static double Score(RealRecord a, RealRecord b, out MatchFlags flags, MatchSettings settings)
    {
        using (settings.Use()) return ScoreCore(a, b, useAddress: true, useVin: true, useEmail: true, out flags, null);
    }

    /// <summary>Explain-mode under <paramref name="settings"/>: the trace a resolve that scored with that
    /// configuration would have produced for the pair, whatever the process-wide <see cref="Flags"/> say.</summary>
    public static double Explain(RealRecord a, RealRecord b, MatchTrace trace, MatchSettings settings)
    {
        using (settings.Use()) return ScoreCore(a, b, useAddress: true, useVin: true, useEmail: true, out _, trace);
    }

    private static double ScoreCore(RealRecord a, RealRecord b, bool useAddress, bool useVin, bool useEmail, out MatchFlags flags, MatchTrace? trace)
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
            trace?.Add("signal", "Names",
                $"\"{trace.Quote(QuotedValue.Name, a.NormName)}\" and \"{trace.Quote(QuotedValue.Name, b.NormName)}\" are {Pct(nameSim)} alike");
        }
        else trace?.Add("signal", "Names", a.NormName.Length == 0 && b.NormName.Length == 0
            ? "no usable name on either record, so names were not compared"
            : $"no usable name on {(a.NormName.Length == 0 ? "record A" : "record B")}, so names were not compared");

        // How the two names are compared token by token: across scripts (Flags.CrossScriptNames) and
        // anchored on the given name (Flags.AnchoredGivenName). Both off: the default comparison.
        var nameCtx = namesBoth && !Flags.Baseline ? NameCtx.Of(a, b) : default;
        if (nameCtx.Cross)
            trace?.Add("signal", "Names in two scripts",
                "one name is written in Arabic letters and the other in Latin letters, so the names were also compared letter by letter across the two scripts");

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
                phoneExact ? $"both records have the same number ({trace.Quote(QuotedValue.Phone, SharedPhone(a, b))})"
                : phoneSuffix ? "one record has the number with a digit missing; the rest agrees with the other record's number"
                : "both records have numbers, but none in common");
        }
        else trace?.Add("signal", "Phone", "no number on one of the records, so numbers were not compared");

        bool idsBoth = a.NationalId is not null && b.NationalId is not null;
        bool idEqual = idsBoth && a.NationalId == b.NationalId;
        if (idsBoth)
        {
            flags |= MatchFlags.IdsBoth;
            if (idEqual) flags |= MatchFlags.IdEqual;
            score += 0.10 * (idEqual ? 1 : 0);
            weight += 0.10;
            trace?.Add("signal", "ID number", idEqual ? "both records have the same ID number" : "the records have different ID numbers (see below)");
        }

        // E-mail — a contact key like phone, but ASYMMETRIC evidence like VIN and address: weight is
        // added ONLY when an address is actually shared, and a non-match NEVER penalizes. One person
        // routinely holds several mailboxes — measured corpora have exactly this shape, one human on a
        // work address at one source and a personal one at another — so scoring a non-match as 0.00 at
        // full weight would actively SPLIT true matches.
        // The same asymmetry makes the signal strictly additive: enabling e-mail on a tenant can only
        // ever raise a pair's confidence, never lower it, so it cannot regress a prior resolve.
        //
        // Comparison is EXACT on the canonical form (see Norm.Email for why fuzzy local-part matching
        // is unsafe). Role/shared mailboxes are removed by MatchEmails before they get here.
        string? sharedEmail = null;
        bool emailExact = false;
        if (useEmail && Flags.EmailMatching && !Flags.Baseline)
        {
            var emailsA = MatchEmails(a);
            var emailsB = MatchEmails(b);
            if (emailsA.Count > 0 && emailsB.Count > 0)
            {
                flags |= MatchFlags.EmailsBoth;
                sharedEmail = emailsA.Find(e => emailsB.Contains(e, StringComparer.Ordinal));
                emailExact = sharedEmail is not null;
                if (emailExact)
                {
                    flags |= MatchFlags.EmailExact;
                    score += 0.45;
                    weight += 0.45;
                    trace?.Add("signal", "E-mail", $"both records have the same address ({trace.Quote(QuotedValue.Email, sharedEmail ?? "")})");
                }
                else trace?.Add("signal", "E-mail",
                    "both records have addresses, but none in common; not counted against them, since one person often has several");
            }
            else if (HasRoleOnlyEmail(a) || HasRoleOnlyEmail(b))
            {
                // "Suppressed" is a different answer from "absent", and the steward has to be able to
                // see which one happened when asking why a pair didn't merge on e-mail.
                flags |= MatchFlags.EmailRoleOnly;
                trace?.Add("signal", "E-mail",
                    "one record's only address is a shared mailbox (info@, sales@ …) that many people use, so it says nothing about who the person is and was not compared");
            }
            else trace?.Add("signal", "E-mail", "no usable address on one of the records, so addresses were not compared");
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
        double servicedVinScore = 0;
        if (vinClass is VinClass.SoldOverlap or VinClass.Serviced)
        {
            double strength = vinClass == VinClass.SoldOverlap ? 1.0 : 0.6;
            if (vinClass == VinClass.Serviced) servicedVinScore = 0.45 * strength;
            score += 0.45 * strength;
            weight += 0.45;
            flags |= vinClass == VinClass.SoldOverlap ? MatchFlags.VinSoldOverlap : MatchFlags.VinServiced;
            trace?.Add("signal", "Vehicle",
                vinClass == VinClass.SoldOverlap
                    ? $"both records bought or own the same vehicle ({sharedVin}) over the same period: strong evidence"
                    : $"the same vehicle ({sharedVin}) was serviced under both records: supporting evidence only, since the driver may be a relative");
        }
        else if (vinClass == VinClass.Transfer)
        {
            flags |= MatchFlags.VinTransfer;
            trace?.Add("signal", "Vehicle", $"the same vehicle ({sharedVin}), but it was sold between the two records' visits: it changed owner, so it says nothing about them being one person");
        }
        else if ((flags & MatchFlags.VinBoth) != 0)
            trace?.Add("signal", "Vehicle", "both records have vehicles, but none in common; not counted against them, since a person may own several");

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
            trace?.Add("signal", "Direct link",
                "one record points straight at the other (for example a ticket raised for this customer): strong evidence");
        }

        if (weight == 0)
        {
            trace?.Add("decide", "Nothing to compare", "the records have nothing in common that can be compared", 0);
            return 0;
        }
        double conf = score / weight;
        trace?.Add("base", "All the evidence together", $"everything above together gives a match strength of {Pct(conf)}", conf);

        // Flags.ServicedVinNeverLowers: a serviced VIN is supporting evidence; it may raise the blend, never lower it.
        if (Flags.ServicedVinNeverLowers && !Flags.Baseline && servicedVinScore > 0 && weight > 0.45 + 1e-9)
        {
            double without = (score - servicedVinScore) / (weight - 0.45);
            if (without > conf)
            {
                conf = without;
                trace?.Add("base", "The serviced vehicle supports, never lowers",
                    $"the shared serviced vehicle is supporting evidence, so the strength is taken without it when that is higher: {Pct(conf)}", conf);
            }
        }

        // Never auto-merge on name alone: one hyper-common two-token name appeared 81x at a single dealer. A perfect
        // name score with no corroborating signal must land mid-steward-queue, well clear of
        // the merge bands (review 2026-06-07: 0.85 parked common-name strangers at band 2).
        // A shared (positive) VIN counts as corroboration — name is no longer the ONLY signal — so
        // it lifts the damp the same way a phone/ID does (two records of one hyper-common name on one serviced car are the
        // corroborated case the damp exists to spare, not the common-name-stranger case it targets).
        // A shared e-mail lifts the damp for the same reason a shared VIN does: name is no longer the
        // ONLY signal. Without this the damp silently defeats the e-mail rule on the exact pairs it
        // exists to catch — two records of one person, same name, same address, no phone on either
        // side (the duplicate-golden shape) would score a perfect 1.00 and then be dragged to
        // 0.70, below every merge band. Gated on a SHARED address, not on merely having one, since a
        // non-match contributes nothing.
        bool vinPositive = vinClass is VinClass.SoldOverlap or VinClass.Serviced;
        if (namesBoth && !phonesBoth && !idsBoth && !vinPositive && !emailExact)
        {
            conf *= 0.70;
            flags |= MatchFlags.NameOnlyDamp;
            trace?.Add("gate", "Only the name in common", "the name is the only thing they share, and many different people have the same name, so the match strength is lowered: a name alone never combines two records", conf);
        }

        // The mirror image (Flags.NamelessNeedsCorroboration; off by default): never auto-merge on a
        // phone alone when there is no name to confirm it. A record without a usable name scores a
        // perfect 1.00 on a shared phone because the phone is then its only comparable attribute — and
        // the phones such records share are the dealer, fleet and family lines that carry nameless
        // purchases of many people. A shared VIN, e-mail, reference or ID still corroborates.
        if (Flags.NamelessNeedsCorroboration && !Flags.Baseline && !namesBoth && phoneExact
            && !vinPositive && !emailExact && !sameAsLinked && !idEqual)
        {
            conf *= 0.70;
            flags |= MatchFlags.NamelessHeld;
            trace?.Add("gate", "Same number, no name",
                "one record has no usable name and the shared number is the only thing in common; a number alone does not say who a record without a name is, so the match strength is lowered", conf);
        }

        // Structured name parts (Flags.StructuredNames; off by default). Where the feed knows the given
        // and family name, a conflict in either is a decision about two people, not drift to be floored
        // over: same given + different surname on one phone is two namesakes, distinct given names on
        // one phone are relatives. Every person floor below is withheld and the pair is capped at the end.
        var parts = Flags.StructuredNames && !Flags.Baseline ? CompareNameParts(a, b) : PartsVerdict.Unknown;
        bool partsVeto = parts is PartsVerdict.GivenConflict or PartsVerdict.FamilyConflict;
        if (parts == PartsVerdict.GivenConflict)
        {
            flags |= MatchFlags.GivenNameConflict;
            trace?.Add("gate", "First names differ",
                $"the first names \"{trace.Quote(QuotedValue.Name, a.GivenName ?? "")}\" and \"{trace.Quote(QuotedValue.Name, b.GivenName ?? "")}\" are different names, not two spellings of one: relatives or strangers, so the records are not combined", conf);
        }
        else if (parts == PartsVerdict.FamilyConflict)
        {
            flags |= MatchFlags.FamilyNameConflict;
            trace?.Add("gate", "Family names differ",
                $"the family names \"{trace.Quote(QuotedValue.Name, a.FamilyName ?? "")}\" and \"{trace.Quote(QuotedValue.Name, b.FamilyName ?? "")}\" are different names: a shared first name makes them namesakes, not one person, so the records are not combined", conf);
        }

        // Name-aware phone decisiveness (review 2026-06-08). Phone is the king key, but equal
        // 0.45/0.45 weighting let a fuzzy name VETO an exact phone — 177 true matches with a
        // shared phone + spelling/token drift ('Hazim Jabir' / 'Hazem Jaber', 'Salam' /
        // 'Salam Ahmed') sat at 0.80-0.90, just under auto-merge. Conversely a shared *family/
        // shop* phone with clearly different names ('Emad Nahir' / 'Ghanim Ahmed') must NOT merge.
        // So gate the exact-phone signal on name consistency in BOTH directions.
        bool nameConsistent = false, nameConflict = false, orgUnresolved = false, givenNamesDiffer = false, orgPersonApart = false;
        // Flags.ShiftedChainConflict: one record's given name is the other's father name — a parent and a
        // child. Decided where the records are tied by a shared phone or a direct reference.
        bool shifted = Flags.ShiftedChainConflict && !Flags.Baseline && namesBoth && (phoneExact || sameAsLinked)
            && ShiftedChain(a.NormName, b.NormName, nameCtx);
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
                if (Flags.OrgNameMerge && !Flags.Baseline && OrgNamesConsistent(a.NormName, b.NormName))
                {
                    // One organisation written two ways (word order, legal form, drift). A side
                    // without an organisation token must consist of the organisation's own words, two
                    // or more — never a lone person name beside a company.
                    nameConsistent = true;
                    flags |= MatchFlags.NameConsistent;
                    conf = Math.Max(conf, 0.91);
                    trace?.Add("gate", "Same number, one company's names",
                        "both names are the same company's, written two ways (word order, legal form or spelling), and they share the number: enough to combine them", conf);
                }
                else if (Flags.OrgNamesBothSides && !Flags.Baseline && IsLikelyOrg(a.NormName) && IsLikelyOrg(b.NormName)
                    && OrgBothSidesConsistent(a.NormName, b.NormName))
                {
                    // Flags.OrgNamesBothSides: both records name an organisation, and it is one organisation.
                    nameConsistent = true;
                    flags |= MatchFlags.NameConsistent;
                    conf = Math.Max(conf, 0.91);
                    trace?.Add("gate", "Same number, one company's names",
                        "both records name a company and it is the same company, written two ways (word order, legal form or spelling), and they share the number: enough to combine them", conf);
                }
                else
                {
                    // Flags.OrgNameMerge decides an organisation line by its names: when they are not
                    // one organisation's, the weighted base must not merge it either (capped below).
                    orgUnresolved = Flags.OrgNameMerge && !Flags.Baseline;
                    // Flags.OrgNamesBothSides: a company and a person (often the one it is named after) are two parties.
                    orgPersonApart = Flags.OrgNamesBothSides && !Flags.Baseline && IsLikelyOrg(a.NormName) != IsLikelyOrg(b.NormName)
                        && NameTokens(IsLikelyOrg(a.NormName) ? b.NormName : a.NormName).Length >= 3;
                    trace?.Add("gate", "Company number",
                        orgPersonApart
                            ? "one record names a company and the other a person, perhaps the one the company is named after: a company and a person are two parties, so the shared number does not combine them"
                            : "the shared number belongs to a company name, and different people use one company number, so the shared number alone does not combine them", conf);
                }
            }
            else if (partsVeto)
            {
                // The structured verdict above already decided "two people"; completely unrelated
                // names still take the stricter name-conflict cap.
                if (NameConflict(a.NormName, b.NormName, nameSim))
                {
                    nameConflict = true;
                    flags |= MatchFlags.NameConflictCap;
                    conf = Math.Min(conf, 0.55);
                    trace?.Add("gate", "Same number, unrelated names",
                        "the names have nothing in common, so the number is shared by different people; kept well short of combining", conf);
                }
                else trace?.Add("gate", "Same number, different names",
                    "the number is shared, but the first or family names are different names, so the number alone is not enough to combine them", conf);
            }
            else if (shifted)
            {
                // Flags.ShiftedChainConflict: decided before name consistency, which reads the father's
                // chain inside the son's as one name unless the names are anchored on the given name.
                givenNamesDiffer = true;
                flags |= MatchFlags.GivenNameConflict;
                trace?.Add("gate", "Same number, a parent and a child",
                    "one record's first name is the other record's father's name: a parent and a child share the number, so these are two people", conf);
            }
            else if (NameConsistent(a.NormName, b.NormName, nameCtx))
            {
                nameConsistent = true;
                flags |= MatchFlags.NameConsistent;
                conf = Math.Max(conf, 0.93);
                trace?.Add("gate", "Same number, same name",
                    "every part of the shorter name matches the longer one, only the spelling differs; with the same number they are one person, enough to combine them", conf);
            }
            else if (GivenNamesMatch(a.NormName, b.NormName, nameCtx)
                && Flags.GivenNameChainFit && !Flags.Baseline && !ChainsFit(a.NormName, b.NormName, nameCtx))
            {
                // Flags.GivenNameChainFit: the first names match but no later name does — another father.
                trace?.Add("gate", "Same number, same first name, another father",
                    "the first names look alike, but none of the other names (father, grandfather) matches, or the first names differ in a way spelling does not explain: a son, a cousin, a brother or a namesake may share the number, so it is not enough to combine them", conf);
            }
            else if (GivenNamesMatch(a.NormName, b.NormName, nameCtx))
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
                trace?.Add("gate", "Same number, same first name",
                    "the first names match (the spelling may differ) and the rest of the name differs: one person written down with different parts of the family name, enough to combine them", conf);
            }
            else if (NameConflict(a.NormName, b.NormName, nameSim))
            {
                nameConflict = true;
                flags |= MatchFlags.NameConflictCap;
                conf = Math.Min(conf, 0.55);
                trace?.Add("gate", "Same number, unrelated names",
                    "the names have nothing in common, so the number is shared by different people; kept well short of combining", conf);
            }
            else if (Flags.DistinctGivenNames && !Flags.Baseline && GivenNamesClearlyDiffer(a.NormName, b.NormName, nameCtx))
            {
                // Flags.DistinctGivenNames: two different first names on one number are two people
                // (siblings, a parent and a child), whatever the rest of the names blend to. Held below
                // the steward line at the end, after every boost.
                givenNamesDiffer = true;
                flags |= MatchFlags.GivenNameConflict;
                trace?.Add("gate", "Same number, different first names",
                    "the number is shared but the first names are different names, not two spellings of one: relatives share a number, so these are two people", conf);
            }
            else trace?.Add("gate", "Same number, different first names",
                "the number is shared but the first names differ: they could be relatives or unrelated people, so the number alone is not enough to combine them", conf);
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
                trace?.Add("address", "Address agrees",
                    $"\"{trace.Quote(QuotedValue.Address, a.NormAddress)}\" and \"{trace.Quote(QuotedValue.Address, b.NormAddress)}\" agree down to the district, a small boost; a different address never counts against them", conf);
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
                // Flags.OrgNamesBothSides: a person rule; an organisation line never takes it. Nor does a parent
                // and a child (Flags.ShiftedChainConflict).
                bool personRuleApplies = !(Flags.OrgNamesBothSides && !Flags.Baseline && (flags & MatchFlags.OrgLine) != 0) && !shifted;
                if (phoneExact && !nameConsistent && namesBoth && !partsVeto && personRuleApplies && FirstTokensAlign(a.NormName, b.NormName, nameCtx))
                {
                    string subA = SubCityRemainder(a), subB = SubCityRemainder(b);
                    if (subA.Length >= 4 && subB.Length >= 4
                        && 0.5 * Sim.Trigram(subA, subB) + 0.5 * Sim.JaroWinkler(subA, subB) >= 0.60)
                    {
                        flags |= MatchFlags.ChainSliceRescue;
                        conf = Math.Max(conf, 0.91);
                        trace?.Add("address", "Same first name, number and district",
                            $"the same first name, the same number, and the district part of the address agrees (\"{trace.Quote(QuotedValue.Address, subA)}\" and \"{trace.Quote(QuotedValue.Address, subB)}\"): one person written down with different parts of the family name, enough to combine them", conf);
                    }
                }
            }
            else trace?.Add("address", "Address differs",
                $"\"{trace.Quote(QuotedValue.Address, a.NormAddress)}\" and \"{trace.Quote(QuotedValue.Address, b.NormAddress)}\" do not agree down to the district; not counted against them, since people move and buy in other cities", conf);
        }
        else if (trace is not null && useAddress && !nameConflict)
        {
            trace.Add("address", "Address not compared",
                a.NormAddress.Length == 0 && b.NormAddress.Length == 0 ? "no address on either record"
                : a.NormAddress.Length == 0 || b.NormAddress.Length == 0 ? "no address on one of the records"
                : "at least one record has only a city, and living in the same city says nothing about being one person", conf);
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
            // Nameless counterpart of the name gate (Flags.NamelessNeedsCorroboration): with no name on
            // a side, the sold VIN is the only identity evidence, and phones that are present on both
            // sides and differ contradict it (a buyer's car brought in by someone else) — no floor.
            // Flags.NamelessSoldVinNeedsPhone, the sold-VIN half on its own: the nameless record's own number
            // must agree; a nameless record with a number beside a record whose number differs or is missing
            // is the car brought in by someone the buyer's record does not confirm.
            bool namelessPhonesDiffer = !namesBoth && !phoneExact && !phoneSuffix && !emailExact
                && ((Flags.NamelessNeedsCorroboration && phonesBoth)
                    || (Flags.NamelessSoldVinNeedsPhone && !Flags.Baseline && NamelessSideHasPhone(a, b)));
            if (partsVeto)
                trace?.Add("vin", "Same vehicle bought, first or family names differ",
                    $"both records bought the same vehicle ({sharedVin}) but the first or family names are different names: relatives share cars and cars are resold, so this alone is not enough to combine them", conf);
            else if (namelessPhonesDiffer)
            {
                flags |= MatchFlags.NamelessHeld;
                // Flags.NamelessSoldVinNeedsPhone: with no number on the named side, the car was the only thing
                // compared; the nameless record's unconfirmed number counts as a number that does not agree.
                if (!phonesBoth) conf = Math.Min(conf, NamelessUnconfirmedCap);
                trace?.Add("vin", "Same vehicle bought, no name, different numbers",
                    $"both records bought the same vehicle ({sharedVin}), but one record has no name and its number is not the other record's: someone else may have brought the buyer's car in, so the vehicle alone is not enough to combine them", conf);
            }
            else if (!namesBoth || NameConsistent(a.NormName, b.NormName, nameCtx))
            {
                conf = Math.Max(conf, 0.91);
                flags |= MatchFlags.VinSoldMerge;
                trace?.Add("vin", "Same vehicle bought, names agree",
                    $"both records bought the same vehicle ({sharedVin}) and the names agree (or one record has no name): enough to combine them", conf);
            }
            else if (Flags.OrgNameMerge && !Flags.Baseline && OrgOnSoldVin(a.NormName, b.NormName))
            {
                // Flags.OrgNameMerge: the buyer is an organisation and the other record names that
                // organisation (word order, word forms, legal form dropped, or its one-word name alone).
                conf = Math.Max(conf, 0.91);
                flags |= MatchFlags.VinSoldMerge;
                trace?.Add("vin", "Same vehicle bought, one company's names",
                    $"both records bought the same vehicle ({sharedVin}) and both names are the same company's, written with or without its legal form: enough to combine them", conf);
            }
            else if (namesBoth && GivenNamesMatch(a.NormName, b.NormName, nameCtx))
                trace?.Add("vin", "Same vehicle bought, similar name",
                    $"both records bought the same vehicle ({sharedVin}) and the first names match, but the rest of the name differs: relatives often share a car, so this alone is not enough to combine them", conf);
            else trace?.Add("vin", "Same vehicle bought, different names",
                $"both records bought the same vehicle ({sharedVin}) but the names point to different people: supporting evidence only, not enough to combine them", conf);
        }

        // Shared SERVICED VIN + the same full name (Flags.ServicedVinNameFloor; off by default). The
        // serviced-VIN objection is "the visitor may be a relative, a driver or a new owner"; a fully
        // consistent name of two or more words on both sides answers it, so the phone no longer has to.
        if (Flags.ServicedVinNameFloor && !Flags.Baseline && useVin && vinClass == VinClass.Serviced && namesBoth
            && !partsVeto && conf < 0.91
            && NameTokens(a.NormName).Length >= 2 && NameTokens(b.NormName).Length >= 2
            && NameConsistent(a.NormName, b.NormName, nameCtx)
            && (!Flags.StrictNameParts || GivenNamesSame(a, b)))
        {
            conf = 0.91;
            flags |= MatchFlags.ServicedVinNameMerge;
            trace?.Add("vin", "Same vehicle serviced, same full name",
                $"the same vehicle ({sharedVin}) was serviced under both records and every part of the shorter name matches the longer one, with at least two parts each: not a relative or a driver, so enough to combine them even if the numbers differ", conf);
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
            // Flags.ShiftedChainConflict: a ticket raised by the son on the father's customer number (or the
            // other way round) is two people, whatever the phones.
            if (shifted && !givenNamesDiffer)
            {
                givenNamesDiffer = true;
                flags |= MatchFlags.GivenNameConflict;
                trace?.Add("gate", "Direct link, a parent and a child",
                    "one record points at the other, but one record's first name is the other record's father's name: a parent and a child, so these are two people", conf);
            }
            // Flags.SameAsRefPhoneFloor: the reference and the exact phone agree; only names that say two
            // people (unrelated, clearly different first names, a parent and a child) stop it.
            bool phoneVouches = Flags.SameAsRefPhoneFloor && !Flags.Baseline && phoneExact && !nameConflict && !givenNamesDiffer
                && !(namesBoth && GivenNamesClearlyDiffer(a.NormName, b.NormName, nameCtx));
            bool namesAgree = !namesBoth || NameConsistent(a.NormName, b.NormName, nameCtx) || GivenNamesMatch(a.NormName, b.NormName, nameCtx);
            if (!partsVeto && !shifted && (namesAgree || phoneVouches))
            {
                conf = Math.Max(conf, namesBoth && NameConsistent(a.NormName, b.NormName, nameCtx) ? 0.95 : 0.91);
                flags |= MatchFlags.SameAsMerge;
                if (namesAgree)
                    trace?.Add("gate", "Direct link, names agree",
                        "one record points at the other and the names agree (or one record has no name): enough to combine them", conf);
                else
                    trace?.Add("gate", "Direct link and the same number",
                        "one record points at the other and they have the same number; the names are written differently (another script, a title, a short form) but do not point to different people: enough to combine them", conf);
            }
            else trace?.Add("gate", "Direct link, names differ",
                "one record points at the other, but the names point to different people (perhaps a ticket raised on the wrong customer), so the link alone is not enough to combine them", conf);
        }

        // Shared e-mail → auto-merge floor, name-gated exactly like the sold-VIN and same-as rules. A
        // personal mailbox is near-king-key grade (it belongs to one person by construction), so let it
        // cross the line with a weak or absent name the way an exact phone does. But data entry puts
        // the WRONG person's address on a record often enough that a genuinely different name has to
        // hold the pair in the steward band: in a measured corpus one dealer-staff mailbox carried
        // three distinct staff names, which is exactly the shape this gate must refuse to merge.
        // No org-line guard here (unlike the phone path): a shared COMPANY mailbox is already removed
        // by the role filter, and two records of one company on one address is a legitimate merge.
        // Sits before the conflict penalties so a national-ID conflict still crushes it (×0.3).
        if (emailExact)
        {
            if (!partsVeto && (!namesBoth || NameConsistent(a.NormName, b.NormName, nameCtx) || GivenNamesMatch(a.NormName, b.NormName, nameCtx)))
            {
                conf = Math.Max(conf, namesBoth && NameConsistent(a.NormName, b.NormName, nameCtx) ? 0.95 : 0.91);
                flags |= MatchFlags.EmailMerge;
                trace?.Add("gate", "Same e-mail, names agree",
                    $"both records have the address ({trace.Quote(QuotedValue.Email, sharedEmail ?? "")}) and the names agree (or one record has no name): enough to combine them", conf);
            }
            else trace?.Add("gate", "Same e-mail, names differ",
                $"both records have the address ({trace.Quote(QuotedValue.Email, sharedEmail ?? "")}) but the names point to different people (an address can be typed on the wrong record, or used by several staff), so the address alone is not enough to combine them", conf);
        }

        // Conflict penalties — only on strong, plausibility-filtered evidence.
        if (idsBoth && !idEqual)
        {
            conf *= 0.3;
            flags |= MatchFlags.IdConflict;
            trace?.Add("conflict", "Different ID numbers", "the ID numbers differ, which is strong evidence of two people, so the match strength is cut sharply", conf);
        }
        if (a.Dob is not null && b.Dob is not null)
        {
            if (a.Dob.EqualsLoose(b.Dob))
            {
                conf = Math.Min(1.0, conf + 0.05);
                flags |= MatchFlags.DobEqual;
                trace?.Add("conflict", "Same birth date", $"the same date of birth ({trace.Quote(QuotedValue.BirthDate, $"{a.Dob.P1}/{a.Dob.P2}/{a.Dob.Year}")}), a small boost", conf);
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
                trace?.Add("conflict", "Different birth dates",
                    $"{trace.Quote(QuotedValue.BirthDate, $"{a.Dob.P1}/{a.Dob.P2}/{a.Dob.Year}")} and {trace.Quote(QuotedValue.BirthDate, $"{b.Dob.P1}/{b.Dob.P2}/{b.Dob.Year}")}: dealer birth dates are often wrong, so this is shown for information and not counted against them", conf);
            }
        }

        // Flags.OrgNameMerge: an organisation line whose names are not one organisation's, and that no
        // floor above decided, is not merged on the weighted base: two bodies whose long titles share
        // every word but the place name score a high name blend on a shared phone. Steward band.
        const MatchFlags decidingFloors = MatchFlags.VinSoldMerge | MatchFlags.SameAsMerge | MatchFlags.EmailMerge
            | MatchFlags.ServicedVinNameMerge | MatchFlags.PhoneNameMerge | MatchFlags.ChainSliceRescue;
        if (orgUnresolved && conf > OrgUnresolvedCap && (flags & decidingFloors) == 0)
        {
            conf = OrgUnresolvedCap;
            trace?.Add("conflict", "Company number, names differ",
                "a company shares the number, but the names are not the same company's, so the records are left for a person to decide", conf);
        }

        // Flags.OrgNamesBothSides: a company and a person on one number are not merged on the weighted base.
        if (orgPersonApart && conf > OrgUnresolvedCap && (flags & decidingFloors) == 0)
        {
            conf = OrgUnresolvedCap;
            trace?.Add("conflict", "A company and a person",
                "one record names a company and the other a person: they share the number, but a person decides whether they belong together", conf);
        }

        // Flags.NamedMergeNeedsFloor: two named records whose names no rule above found to be one person's
        // are not merged on the blend of similar names, a phone and a boost.
        if (Flags.NamedMergeNeedsFloor && !Flags.Baseline && namesBoth && !idEqual && conf > OrgUnresolvedCap
            && (flags & (MatchFlags.OrgLine | MatchFlags.NameConsistent | decidingFloors)) == 0
            && !NameConsistent(a.NormName, b.NormName, nameCtx))
        {
            conf = OrgUnresolvedCap;
            trace?.Add("conflict", "Similar names, not one name",
                "the names look alike but no rule found them to be one person's name (the first names or the rest of the names differ), so a person decides", conf);
        }

        // Flags.DistinctGivenNames: different first names on one number are decided like a structured
        // given-name conflict — below the steward line, unless a decisive floor above combined them.
        if (givenNamesDiffer && conf > PartsConflictCap && (flags & decidingFloors) == 0)
        {
            conf = PartsConflictCap;
            trace?.Add("conflict", "First names differ",
                "the first names are different names, so these are two people: nothing else they share brings them up for a person to decide", conf);
        }

        // Last, so no boost above (address, DOB) can lift a structured name conflict back into the
        // steward band: the pair is decided, and a queue full of obvious relatives is queue noise.
        if (partsVeto && conf > PartsConflictCap)
        {
            conf = PartsConflictCap;
            trace?.Add("conflict", "First or family names differ",
                "the first or family names are different names, so these are two people: nothing else they share brings them up for a person to decide", conf);
        }

        trace?.Add("decide", CaseLabels.Decision(conf).Label, $"final match strength {Pct(conf)}", conf);
        return conf;
    }

    /// <summary>
    /// Flags.DistinctGivenNames: the first names are two names, not one name written two ways. Leading
    /// titles (mr, mrs, ms, miss, dr, eng) are set aside first. Not two names when: the names have a
    /// different number of words but read alike with their spaces removed (one name split or joined:
    /// "abc def" / "abcdef"); a first name of one or two letters; one first name starts the other (a first
    /// name joined to the next word); the
    /// first names align (<see cref="TokensAlign"/>, or across scripts); or they are near spellings (a
    /// Jaro-Winkler of 0.80 or more with consonant skeletons at most two edits apart, or skeletons of one
    /// length one letter apart). Only first names that fail every one of these tests are different names.
    /// </summary>
    private static bool GivenNamesClearlyDiffer(string a, string b, NameCtx ctx)
    {
        var ta = WithoutTitles(MatchTokens(a, ctx));
        var tb = WithoutTitles(MatchTokens(b, ctx));
        if (ta.Length == 0 || tb.Length == 0) return false;
        if (ta.Length != tb.Length)
        {
            string ja = string.Concat(ta), jb = string.Concat(tb);
            if (ja.Contains(jb, StringComparison.Ordinal) || jb.Contains(ja, StringComparison.Ordinal) || Sim.JaroWinkler(ja, jb) >= 0.85) return false;
        }
        string ga = ta[0], gb = tb[0];
        if (ga.Length <= 2 || gb.Length <= 2) return false;   // too short to tell a name from a slip
        if (ga.StartsWith(gb, StringComparison.Ordinal) || gb.StartsWith(ga, StringComparison.Ordinal)) return false;
        if (TokensAlign(ga, gb)) return false;
        int skeletonEdits = SkeletonDistance(ga, gb);
        if (Sim.JaroWinkler(ga, gb) >= 0.80 && skeletonEdits <= 2) return false;
        if (skeletonEdits <= 1 && Skeleton(Canon(ga)).Length == Skeleton(Canon(gb)).Length) return false;
        if (ctx.Cross && (ctx.ArabicSide == 1 ? CrossAligns(ga, gb, 1) : CrossAligns(gb, ga, 1))) return false;
        return true;
    }

    /// <summary>
    /// Flags.GivenNameChainFit: the two name chains can be one person's. The first names must not differ by
    /// a dropped first consonant (a romanization slip changes a letter, it does not drop the name's first
    /// consonant: two such names are two names). A given name alone fits any chain; two two-word names fit
    /// unless <see cref="Flags.TwoWordChainFit"/> is on; otherwise at least one later token (father,
    /// grandfather, family) of one name aligns with a later token of the other, in any order
    /// (<see cref="LaterTokensAlign"/>). A later token repeating the given name is filler ("Given Given"),
    /// and a later token made of the article "al" and four letters or more is a family or tribal name that
    /// one record may leave out; neither counts as a later token.
    /// </summary>
    private static bool ChainsFit(string a, string b, NameCtx ctx)
    {
        var ta = WithoutTitles(MatchTokens(a, ctx));
        var tb = WithoutTitles(MatchTokens(b, ctx));
        if (ta.Length == 0 || tb.Length == 0) return true;
        // Also when one first name merely contains the other (the shared token rule's containment).
        if (FirstConsonantDropped(ta[0], tb[0])) return false;
        var la = Later(ta);
        var lb = Later(tb);
        if (la.Length == 0 || lb.Length == 0) return true;
        if (la.Length == 1 && lb.Length == 1 && !Flags.TwoWordChainFit) return true;
        foreach (var x in la)
            foreach (var y in lb)
                if (LaterTokensAlign(x, y, ctx.ArabicSide)) return true;
        return false;

        static string[] Later(string[] t) =>
            [.. t.Skip(1).Where(x => x != t[0] && !(x.Length >= 6 && x.StartsWith("al", StringComparison.Ordinal)))];

        // One name's consonant skeleton is the other's with a first consonant other than h in front (the
        // glottal h is romanization drift; a fused article "al" is not a consonant of the name); both of two
        // consonants or more.
        static bool FirstConsonantDropped(string x, string y)
        {
            string kx = Skeleton(Canon(x)), ky = Skeleton(Canon(y));
            var (s, l, lt) = kx.Length < ky.Length ? (kx, ky, y) : (ky, kx, x);
            return l.Length == s.Length + 1 && s.Length >= 2 && l[0] != 'h' && l[1..] == s
                && !lt.StartsWith("al", StringComparison.Ordinal);
        }
    }

    /// <summary>Two later name tokens (x from record A, y from record B) can be one name for
    /// <see cref="Flags.GivenNameChainFit"/>: the shared token rule, romanization drift at the given-name bar
    /// (Jaro-Winkler 0.80 and consonant skeletons one edit apart), the same consonant skeleton once the
    /// letters one name is romanized with are read alike (ch and j; q, k and g; v and f; d and z), an initial
    /// and the name it starts, a truncated name of three letters or more and the name it begins, or across
    /// scripts. Deliberately loose: the question is only whether the chains could be one person's.</summary>
    private static bool LaterTokensAlign(string x, string y, int arabicSide)
    {
        if (TokensAlign(x, y)) return true;
        if (x.Length == 1 || y.Length == 1)
            return (x.Length == 1 && y.StartsWith(x, StringComparison.Ordinal)) || (y.Length == 1 && x.StartsWith(y, StringComparison.Ordinal));
        if (Sim.JaroWinkler(x, y) >= 0.80 && SkeletonDistance(x, y) <= 1) return true;
        string sx = Skeleton(CrossLetters(Canon(x).Replace("ch", "j", StringComparison.Ordinal)));
        string sy = Skeleton(CrossLetters(Canon(y).Replace("ch", "j", StringComparison.Ordinal)));
        if (sx == sy && (sx.Length >= 2 || (sx.Length == 1 && x.Length <= 4 && y.Length <= 4))) return true;
        if (x.Length >= 3 && y.Length >= 3 && (x.StartsWith(y, StringComparison.Ordinal) || y.StartsWith(x, StringComparison.Ordinal))) return true;
        return arabicSide switch { 1 => CrossAligns(x, y, 2), 2 => CrossAligns(y, x, 2), _ => false };
    }

    /// <summary>
    /// Flags.ShiftedChainConflict: one name's given name is the other's second word (its father's name)
    /// and the given names differ — a parent and a child ("Son Father Grandfather" against "Father
    /// Grandfather ..."). Leading titles are set aside. Two two-word names in reverse order ("A B" against
    /// "B A") are not a shift: as often one name typed in the other field order.
    /// </summary>
    private static bool ShiftedChain(string a, string b, NameCtx ctx)
    {
        var ta = WithoutTitles(MatchTokens(a, ctx));
        var tb = WithoutTitles(MatchTokens(b, ctx));
        if (ta.Length < 2 || tb.Length < 2) return false;
        // The same given name (any spelling), or one name written with some words joined: not a shift.
        if (TokensAlign(ta[0], tb[0]) || Cross(ta[0], tb[0])) return false;
        string ja = string.Concat(ta), jb = string.Concat(tb);
        if (Math.Abs(ja.Length - jb.Length) <= 2 && Sim.JaroWinkler(ja, jb) >= 0.85) return false;
        if (ta.Length == 2 && tb.Length == 2 && Same(ta[0], tb[1]) && Same(ta[1], tb[0])) return false;
        return Same(ta[1], tb[0]) || Same(ta[0], tb[1]);

        // x from record A, y from record B: one name, not one containing the other.
        bool Same(string x, string y) => x.Length >= 3 && y.Length >= 3 && ((x[0] == y[0] && OneWordJoined(x, y)) || Cross(x, y));
        bool Cross(string x, string y) => (ctx.ArabicSide == 1 && CrossAligns(x, y, 2)) || (ctx.ArabicSide == 2 && CrossAligns(y, x, 2));
    }

    /// <summary>Words that say what kind of business a name is, not which one: an organisation name made of
    /// these alone is no name (<see cref="Flags.OrgNamesBothSides"/>).</summary>
    private static readonly HashSet<string> GenericBusinessWords = new(StringComparer.Ordinal)
    {
        "co", "ltd", "llc", "inc", "corp", "est", "auto", "autos", "car", "cars", "motor", "motors", "trading", "trade",
        "general", "for", "and", "the", "of", "service", "services", "contracting", "international", "investment",
        "investments", "gallery", "exhibition", "center", "centre", "agency", "office", "show", "room", "branch", "new",
    };

    /// <summary>
    /// Flags.OrgNamesBothSides: two organisation names (each carries an organisation token) are one
    /// organisation's. Either the names without their organisation tokens are the same once written without
    /// spaces (a typing slip per ten letters allowed); or every distinctive word of the name with fewer of
    /// them (organisation tokens and generic business words set aside, a leading article "al" dropped) aligns
    /// with a distinctive word of the other, as a token or as a form of one word.
    /// </summary>
    private static bool OrgBothSidesConsistent(string a, string b)
    {
        var na = NameTokens(a).Where(t => !OrgTokens.Contains(t)).ToArray();
        var nb = NameTokens(b).Where(t => !OrgTokens.Contains(t)).ToArray();
        if (na.Length == 0 || nb.Length == 0) return false;
        string ja = string.Concat(na), jb = string.Concat(nb);
        if (ja.Length >= 6 && jb.Length >= 6 && (ja == jb || Levenshtein(ja, jb) <= Math.Max(1, Math.Min(ja.Length, jb.Length) / 10))) return true;
        var ca = Core(na);
        var cb = Core(nb);
        if (ca.Length == 0 || cb.Length == 0) return false;
        var (small, big) = ca.Length <= cb.Length ? (ca, cb) : (cb, ca);
        return small.All(t => big.Any(u => TokensAlign(t, u) || SharedStem(t, u)));

        static string[] Core(string[] tokens) =>
            [.. tokens.Where(t => !GenericBusinessWords.Contains(t))
                      .Select(t => t.Length >= 5 && t.StartsWith("al", StringComparison.Ordinal) ? t[2..] : t)
                      .Where(t => t.Length >= 3)];

        static bool SharedStem(string t, string u)
        {
            int p = 0, n = Math.Min(t.Length, u.Length);
            while (p < n && t[p] == u[p]) p++;
            return p >= 5 && p >= 0.7 * n;
        }
    }

    /// <summary>
    /// Flags.JoinedNames: the names align when words written together on one side ("givenfather") are
    /// compared with the run of words they stand for on the other ("given father"), joined: from the given
    /// name on, in order, until one name is used up; a joined comparison needs six letters or more on both
    /// sides. The plain one-word-per-word comparison has already failed when this is asked.
    /// </summary>
    private static bool JoinedConsistent(string[] x, string[] y, Func<string, string, bool> align, Func<string, string, bool> alignJoined)
    {
        return Walk(0, 0, false);

        bool Walk(int i, int j, bool joined)
        {
            if (i == x.Length || j == y.Length) return joined && i > 0 && j > 0;
            if (align(x[i], y[j]) && Walk(i + 1, j + 1, joined)) return true;
            for (int k = 2; k <= 3; k++)
            {
                if (j + k <= y.Length)
                {
                    string run = string.Concat(y[j..(j + k)]);
                    if (x[i].Length >= 6 && run.Length >= 6 && alignJoined(x[i], run) && Walk(i + 1, j + k, true)) return true;
                }
                if (i + k <= x.Length)
                {
                    string run = string.Concat(x[i..(i + k)]);
                    if (y[j].Length >= 6 && run.Length >= 6 && alignJoined(run, y[j]) && Walk(i + k, j + 1, true)) return true;
                }
            }
            return false;
        }
    }

    /// <summary>Flags.JoinedNames: a word and a run of words written together are one spelling — the same
    /// letters up to vowel and doubled-letter drift (one consonant skeleton and a near Jaro-Winkler, or one
    /// 'h' apart), never one merely containing the other.</summary>
    private static bool OneWordJoined(string word, string run)
    {
        if (word == run) return true;
        string cw = Canon(word), cr = Canon(run);
        if (Math.Abs(cw.Length - cr.Length) > Math.Max(2, Math.Min(cw.Length, cr.Length) / 4)) return false;
        double jw = Sim.JaroWinkler(cw, cr);
        string sw = Skeleton(cw), sr = Skeleton(cr);
        return (sw == sr && jw >= 0.80) || (jw >= 0.80 && DiffersByOneH(sw, sr)) || (jw >= 0.92 && Levenshtein(sw, sr) <= 1);
    }

    /// <summary>The tokens without leading titles (mr, mrs, ms, miss, dr, eng); a title that is the whole
    /// name stays. Used where the given name is compared as such (<see cref="Flags.AnchoredGivenName"/>,
    /// <see cref="Flags.DistinctGivenNames"/>).</summary>
    private static string[] WithoutTitles(string[] t)
    {
        int i = 0;
        while (i < t.Length - 1 && t[i] is "mr" or "mrs" or "ms" or "miss" or "dr" or "eng") i++;
        return i == 0 ? t : t[i..];
    }

    /// <summary>Where Flags.NamelessSoldVinNeedsPhone holds a nameless sold-VIN pair whose named side has no phone:
    /// the strength a shared car has beside a number that does not agree.</summary>
    public const double NamelessUnconfirmedCap = 0.50;

    /// <summary>A record without a usable name in the pair carries a phone (either one, when neither is named).</summary>
    private static bool NamelessSideHasPhone(RealRecord a, RealRecord b) =>
        (a.NormName.Length == 0 && a.Phones.Length + a.WeakPhones.Length > 0)
        || (b.NormName.Length == 0 && b.Phones.Length + b.WeakPhones.Length > 0);

    /// <summary>Where a structured given/family-name conflict holds a pair (Flags.StructuredNames):
    /// below the steward line, inside the near-miss band so the pair stays browsable.</summary>
    public const double PartsConflictCap = 0.75;

    /// <summary>Where an organisation line with unresolved names is held (Flags.OrgNameMerge): the
    /// steward band, below the auto-merge line.</summary>
    public const double OrgUnresolvedCap = 0.89;

    private enum PartsVerdict { Unknown, Agree, GivenConflict, FamilyConflict }

    /// <summary>
    /// Compare the feed-supplied name parts (Flags.StructuredNames). A part is compared only when both
    /// records carry it. A part typed into the other field explains a difference instead of making
    /// one: with both parts on both sides the given and family names must match crosswise; with a part
    /// missing on one side, either crosswise match is enough (a lone given field holding the surname).
    /// Given and family names are compared under <see cref="Flags.StrictNameParts"/> when it is on;
    /// family names in their two sex forms conflict under <see cref="Flags.FamilyNameSexEndings"/>
    /// unless the given names on both sides align.
    /// </summary>
    private static PartsVerdict CompareNameParts(RealRecord a, RealRecord b)
    {
        string? ga = Part(a.GivenName), gb = Part(b.GivenName), fa = Part(a.FamilyName), fb = Part(b.FamilyName);
        bool givenBoth = ga is not null && gb is not null, familyBoth = fa is not null && fb is not null;
        if (!givenBoth && !familyBoth) return PartsVerdict.Unknown;
        bool givenOk = !givenBoth || StrictPartsAlign(ga!, gb!, given: true);
        bool familyOk = !familyBoth || (StrictPartsAlign(fa!, fb!, given: false) && ((givenBoth && givenOk) || !FamilySexForms(fa!, fb!)));
        if (givenOk && familyOk) return PartsVerdict.Agree;

        bool swapped = ga is not null && fa is not null && gb is not null && fb is not null
            ? CrossAlign(ga, fb) && CrossAlign(fa, gb)
            : (ga is not null && fb is not null && CrossAlign(ga, fb)) || (gb is not null && fa is not null && CrossAlign(gb, fa));
        if (swapped) return PartsVerdict.Agree;
        return !givenOk ? PartsVerdict.GivenConflict : PartsVerdict.FamilyConflict;

        static string? Part(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
        static bool CrossAlign(string x, string y) => NamePartsAlign(x, y) && !FamilySexForms(x, y);
    }

    /// <summary>Two structured given names, or two family names. By default <see cref="NamePartsAlign"/>.
    /// Under <see cref="Flags.StrictNameParts"/>, two names that are each established in the tenant's
    /// corpus (<see cref="Flags.EstablishedNameParts"/>) must be one name (<see cref="SameNamePart"/>):
    /// two attested names that merely look alike are two people. A name the corpus has not established
    /// may be a typing slip of the other one, and keeps the default comparison.</summary>
    private static bool StrictPartsAlign(string x, string y, bool given)
    {
        if (!Flags.StrictNameParts || Flags.Baseline) return NamePartsAlign(x, y);
        if (SameNamePart(x, y, given)) return true;
        return NamePartsAlign(x, y) && !(Flags.EstablishedNameParts.Contains(x) && Flags.EstablishedNameParts.Contains(y));
    }

    /// <summary>
    /// Flags.StrictNameParts: one name, not two names that look alike. Equal; written with or without
    /// its spaces; or, compared after the tenant's <see cref="Flags.NamePartFolds"/>, both as written and
    /// with one of its endings set aside (a given name's <see cref="Flags.GivenNameSuffixes"/>, a family
    /// name's <see cref="Flags.FamilyNameEndings"/>): a near match with the same consonant skeleton
    /// (vowel and doubled-letter drift) or one 'h' apart. A given name may also be a short form of the
    /// other: contained in it, or its skeleton (three or more consonants) starting the other's; a family
    /// name contained in another is another family. A high Jaro-Winkler on its own is not enough: it
    /// rewards a shared first element, and two compound names that share one are two names.
    /// </summary>
    internal static bool SameNamePart(string x, string y, bool given = true)
    {
        if (x == y) return true;
        if (x.Contains(' ') || y.Contains(' '))
        {
            if (x.Replace(" ", "") == y.Replace(" ", "")) return true;
            return NameConsistent(x, y);
        }
        string cx = Canon(x), cy = Canon(y);
        if (OneName(cx, cy, given) || OneName(PartFold(cx), PartFold(cy), given)) return true;
        string px = Strip(cx, given ? Flags.GivenNameSuffixes : Flags.FamilyNameEndings);
        string py = Strip(cy, given ? Flags.GivenNameSuffixes : Flags.FamilyNameEndings);
        if ((!ReferenceEquals(px, cx) || !ReferenceEquals(py, cy)) && OneName(PartFold(px), PartFold(py), given)) return true;
        // One 'h' apart, before the letter folds (which may turn 'h' into another letter).
        return Sim.JaroWinkler(cx, cy) >= 0.80 && DiffersByOneH(Skeleton(cx), Skeleton(cy));

        static bool Contained(string u, string v) => u.Length >= 4 && v.Length >= 4 && (u.Contains(v) || v.Contains(u));

        // Short forms (one contained in the other, or a skeleton that starts the other) are a given
        // name's: a family name contained in another is a different family name.
        static bool OneName(string u, string v, bool shortForms)
        {
            if (u == v || (shortForms && Contained(u, v))) return true;
            string su = Skeleton(u), sv = Skeleton(v);
            if (su.Length > 0 && su == sv) return Sim.JaroWinkler(u, v) >= 0.80;
            if (!shortForms) return false;
            var (shortSk, longSk) = su.Length <= sv.Length ? (su, sv) : (sv, su);
            return shortSk.Length >= 3 && longSk.StartsWith(shortSk, StringComparison.Ordinal);
        }

        static string Strip(string t, IReadOnlyList<string> endings)
        {
            foreach (var s in endings)
                if (s.Length > 0 && t.Length - s.Length >= 3 && t.EndsWith(s, StringComparison.Ordinal)) return t[..^s.Length];
            return t;
        }

        static string PartFold(string t)
        {
            foreach (var (from, to) in Flags.NamePartFolds)
                if (from.Length > 0 && t.Contains(from, StringComparison.Ordinal)) t = t.Replace(from, to, StringComparison.Ordinal);
            return t;
        }
    }

    /// <summary>The serviced-VIN floor's test under <see cref="Flags.StrictNameParts"/>: when both
    /// records carry a structured given name, it is one name (<see cref="SameNamePart"/>), straight or,
    /// with both parts on both sides, crosswise (given and family typed into each other's fields).
    /// Records without a structured given name (organisations, names in an unknown order) pass.</summary>
    private static bool GivenNamesSame(RealRecord a, RealRecord b)
    {
        string? ga = a.GivenName, gb = b.GivenName, fa = a.FamilyName, fb = b.FamilyName;
        if (string.IsNullOrWhiteSpace(ga) || string.IsNullOrWhiteSpace(gb)) return true;
        if (SameNamePart(ga, gb)) return true;
        return !string.IsNullOrWhiteSpace(fa) && !string.IsNullOrWhiteSpace(fb)
            && SameNamePart(ga, fb, given: false) && SameNamePart(fa, gb, given: false);
    }

    /// <summary>Flags.FamilyNameSexEndings: one family name in its masculine and its feminine form
    /// (the stems, at least three letters each, align as name parts).</summary>
    private static bool FamilySexForms(string x, string y)
    {
        if (Flags.FamilyNameSexEndings.Count == 0 || Flags.Baseline || x.Contains(' ') || y.Contains(' ')) return false;
        foreach (var (m, f) in Flags.FamilyNameSexEndings)
            if (Forms(x, y, m, f) || Forms(y, x, m, f)) return true;
        return false;

        static bool Forms(string masc, string fem, string m, string f) =>
            m.Length > 0 && f.Length > 0
            && masc.EndsWith(m, StringComparison.Ordinal) && fem.EndsWith(f, StringComparison.Ordinal)
            && masc.Length - m.Length >= 3 && fem.Length - f.Length >= 3
            && NamePartsAlign(masc[..^m.Length], fem[..^f.Length]);
    }

    /// <summary>Two name parts are one person's: the shared token rule, or romanization drift at the
    /// given-name bar (Jaro-Winkler ≥ 0.80 with consonant skeletons at most one edit apart). A
    /// multi-token part is compared with <see cref="NameConsistent"/>.</summary>
    public static bool NamePartsAlign(string x, string y)
    {
        if (x.Contains(' ') || y.Contains(' ')) return NameConsistent(x, y);
        return TokensAlign(x, y) || (Sim.JaroWinkler(x, y) >= 0.80 && SkeletonDistance(x, y) <= 1);
    }

    /// <summary>A strength or similarity as a whole percentage, the way the case browser shows scores.</summary>
    private static string Pct(double x) => $"{Math.Round(x * 100, MidpointRounding.AwayFromZero):0}%";

    private static string SharedPhone(RealRecord a, RealRecord b) =>
        a.Phones.FirstOrDefault(p => b.Phones.Contains(p))
        ?? a.WeakPhones.FirstOrDefault(p => b.WeakPhones.Contains(p)) ?? "?";

    // Same person under spelling drift vs. two relatives on one phone is decided PER TOKEN, not by
    // an overall blend: every token of the shorter name must align to a token of the longer one.
    // 'Hazim Jabir'/'Hazem Jaber' aligns (both tokens drift); 'Hogr Jafar'/'Hogr Jamil' does NOT
    // (first identical, second a different word) — that's a sibling pair, left to the steward band.
    public static bool NameConsistent(string a, string b) => NameConsistent(a, b, default);

    /// <summary>Two person records' names are consistent (<see cref="NameConsistent(string, string)"/>),
    /// compared under the tenant's cross-script and anchored-given-name rules where they are on.</summary>
    public static bool NameConsistent(RealRecord a, RealRecord b) =>
        NameConsistent(a.NormName, b.NormName, Flags.Baseline ? default : NameCtx.Of(a, b));

    private static bool NameConsistent(string a, string b, NameCtx ctx)
    {
        if (NameConsistentCore(a, b, ctx)) return true;
        if (!Flags.JoinedNames || Flags.Baseline) return false;
        // Flags.JoinedNames: some words written together on one side.
        var ta = MatchTokens(a, ctx);
        var tb = MatchTokens(b, ctx);
        if (ctx.Anchored) { ta = WithoutTitles(ta); tb = WithoutTitles(tb); }
        if (ta.Length == 0 || tb.Length == 0) return false;
        return JoinedConsistent(ta, tb, (x, y) => TokensAlign(x, y)
            || (ctx.ArabicSide == 1 && CrossAligns(x, y, 1)) || (ctx.ArabicSide == 2 && CrossAligns(y, x, 1)),
            (x, y) => OneWordJoined(x, y)
            || (ctx.ArabicSide == 1 && CrossAligns(x, y, 3)) || (ctx.ArabicSide == 2 && CrossAligns(y, x, 3)));
    }

    private static bool NameConsistentCore(string a, string b, NameCtx ctx)
    {
        var ta = MatchTokens(a, ctx);
        var tb = MatchTokens(b, ctx);
        // Flags.AnchoredGivenName: the anchor is the given name, so a leading title is not compared.
        if (ctx.Anchored) { ta = WithoutTitles(ta); tb = WithoutTitles(tb); }
        if (ta.Length == 0 || tb.Length == 0) return false;
        bool aSmall = ta.Length <= tb.Length;
        var (small, big) = aSmall ? (ta, tb) : (tb, ta);
        // Flags.CrossScriptNames: a token of the shorter name may also align across the two scripts. Every
        // token of the shorter name has to align, so a short consonant frame (one or two consonants)
        // counts here only beside the name's other tokens (given and father name together).
        bool smallIsArabic = ctx.ArabicSide == (aSmall ? 1 : 2);
        Func<string, string, bool> align = ctx.Cross
            ? (t, u) => TokensAlign(t, u) || (smallIsArabic ? CrossAligns(t, u, 1) : CrossAligns(u, t, 1))
            : TokensAlign;
        // A single distinct token — a true mononym ('Salam') or a repeated junk name
        // ('Mohammed Mohammed') — requires that many EXACT occurrences in the longer name. So
        // 'Salam' still matches 'Salam Ahmed', but 'Mohammed Mohammed' does NOT match 'Mohammed
        // Thany' (only one 'mohammed'), which was the one residual false-merge (review 2026-06-08).
        if (small.Distinct().Count() < 2)
        {
            string tok = small[0];
            // Flags.AnchoredGivenName: a one-word name is the longer name's given name.
            if (big.Count(t => t == tok) >= small.Length && (!ctx.Anchored || big[0] == tok)) return true;
            // Flags.CrossScriptNames: a lone name in the other script aligns the longer name's given
            // name, and only with a consonant frame of three or more.
            return ctx.Cross && small.Length == 1
                && (smallIsArabic ? CrossAligns(tok, big[0], 3) : CrossAligns(big[0], tok, 3));
        }
        if (ctx.Anchored) return AlignsAnchored(small, big, align);
        if (Flags.StrictTokenAlignment && !Flags.Baseline) return AlignsOneToOne(small, big, align);
        return small.All(t => big.Any(u => align(t, u)));
    }

    /// <summary>Flags.AnchoredGivenName: the shorter name's first token aligns the longer name's first
    /// token, and its other tokens align the longer name's other tokens in order (tokens of the longer
    /// name may be skipped, so a trailing family or tribal name still aligns). Taking the earliest match
    /// is optimal for an order-preserving match, so the scan is greedy.</summary>
    private static bool AlignsAnchored(string[] small, string[] big, Func<string, string, bool> align)
    {
        if (!align(small[0], big[0])) return false;
        int j = 1;
        for (int i = 1; i < small.Length; i++)
        {
            while (j < big.Length && !align(small[i], big[j])) j++;
            if (j == big.Length) return false;
            j++;
        }
        return true;
    }

    /// <summary>
    /// How two person records' names are compared; the default value is the plain token comparison.
    /// <see cref="ArabicSide"/>: under <see cref="Flags.CrossScriptNames"/>, which record's name is
    /// written in Arabic script when the other's is in Latin letters (1 = a, 2 = b, 0 = not a
    /// cross-script pair). <see cref="Anchored"/>: <see cref="Flags.AnchoredGivenName"/>.
    /// </summary>
    private readonly record struct NameCtx(int ArabicSide, bool Anchored)
    {
        public bool Cross => ArabicSide != 0;

        public static NameCtx Of(RealRecord a, RealRecord b)
        {
            int side = 0;
            if (Flags.CrossScriptNames)
            {
                int sa = ScriptOf(a), sb = ScriptOf(b);
                side = sa == 1 && sb == 2 ? 1 : sa == 2 && sb == 1 ? 2 : 0;
            }
            return new NameCtx(side, Flags.AnchoredGivenName);
        }
    }

    /// <summary>The script a record's name is written in: 1 = Arabic letters only (or a repaired CP1256
    /// name), 2 = Latin letters only, 0 = both, neither, or no name.</summary>
    private static int ScriptOf(RealRecord r)
    {
        if (r.NameWasMojibake) return 1;
        bool arabic = false, latin = false;
        foreach (char c in r.RawName)
        {
            if (c is (>= '؀' and <= 'ۿ') or (>= 'ݐ' and <= 'ݿ') or (>= 'ﭐ' and <= '﷿') or (>= 'ﹰ' and <= '﻿'))
                arabic = true;
            else if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')) latin = true;
        }
        return arabic == latin ? 0 : arabic ? 1 : 2;
    }

    /// <summary>The tokens names are compared with: <see cref="NameTokens"/>, and across scripts an "abd"
    /// compound (the word "abd" with or without its linking vowel, then a name) joined into one token,
    /// since one script joins it and the other splits it.</summary>
    private static string[] MatchTokens(string name, NameCtx ctx)
    {
        var t = NameTokens(name);
        if (!ctx.Cross || t.Length < 2) return t;
        // Flags.CrossScriptLetters: a compound whose second word is "allah" or "din" is also one token.
        bool compounds = Flags.CrossScriptLetters && !Flags.Baseline && t.Skip(1).Any(CompoundTails.Contains);
        if (!t.Any(IsAbdPrefix) && !compounds) return t;
        var joined = new List<string>(t.Length);
        for (int i = 0; i < t.Length; i++)
            if (IsAbdPrefix(t[i]) && i + 1 < t.Length) { joined.Add(t[i] + t[i + 1]); i++; }
            else if (compounds && i + 1 < t.Length && t[i].Length >= 3 && CompoundTails.Contains(t[i + 1])) { joined.Add(t[i] + t[i + 1]); i++; }
            else joined.Add(t[i]);
        return [.. joined];

        static bool IsAbdPrefix(string x) => x == "abd" || AbdPrefixes.Contains(x);
    }

    /// <summary>Flags.CrossScriptLetters: second words of a compound name that one script joins to the first
    /// word and the other writes apart ("allah", "din" in their spellings).</summary>
    private static readonly HashSet<string> CompoundTails = new(StringComparer.Ordinal)
    {
        "allah", "alla", "ullah", "ulla", "llah", "allh", "aldin", "addin", "eddin", "uddin", "aldeen", "eldin", "aldyn", "din", "deen",
    };

    /// <summary>Flags.StrictTokenAlignment: every token of <paramref name="small"/> aligns to its OWN
    /// token of <paramref name="big"/> (bipartite matching, augmenting paths). The default any-to-any
    /// test lets a given name contained in the family name ('Name' in 'Nameson') align twice, which
    /// makes 'Name Nameson' consistent with every 'Other Nameson' on the same phone.</summary>
    private static bool AlignsOneToOne(string[] small, string[] big) => AlignsOneToOne(small, big, TokensAlign);

    private static bool AlignsOneToOne(string[] small, string[] big, Func<string, string, bool> align)
    {
        var owner = new int[big.Length];
        Array.Fill(owner, -1);
        for (int i = 0; i < small.Length; i++)
            if (!Augment(i, new bool[big.Length])) return false;
        return true;

        bool Augment(int i, bool[] seen)
        {
            for (int j = 0; j < big.Length; j++)
            {
                if (seen[j] || !align(small[i], big[j])) continue;
                seen[j] = true;
                if (owner[j] < 0 || Augment(owner[j], seen)) { owner[j] = i; return true; }
            }
            return false;
        }
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

    /// <summary><see cref="FirstTokensAlign(string, string)"/> for two person records, under the tenant's
    /// cross-script rule where it is on.</summary>
    public static bool FirstTokensAlign(RealRecord a, RealRecord b) =>
        FirstTokensAlign(a.NormName, b.NormName, Flags.Baseline ? default : NameCtx.Of(a, b));

    private static bool FirstTokensAlign(string a, string b, NameCtx ctx) =>
        FirstTokensAlign(a, b) || (ctx.Cross && CrossGivenNames(a, b, ctx));

    /// <summary>Flags.CrossScriptNames: the given names align across the two scripts, on their own with a
    /// consonant frame of three or more, a shorter frame only together with the father name.</summary>
    private static bool CrossGivenNames(string a, string b, NameCtx ctx)
    {
        var ta = MatchTokens(a, ctx);
        var tb = MatchTokens(b, ctx);
        if (ta.Length == 0 || tb.Length == 0) return false;
        var (ar, la) = ctx.ArabicSide == 1 ? (ta, tb) : (tb, ta);
        if (CrossAligns(ar[0], la[0], 3)) return true;
        return ar.Length >= 2 && la.Length >= 2 && CrossAligns(ar[0], la[0], 1)
            && (TokensAlign(ar[1], la[1]) || CrossAligns(ar[1], la[1], 1));
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

    /// <summary><see cref="GivenNamesMatch(string, string)"/> for two person records, under the tenant's
    /// cross-script rule where it is on.</summary>
    public static bool GivenNamesMatch(RealRecord a, RealRecord b) =>
        GivenNamesMatch(a.NormName, b.NormName, Flags.Baseline ? default : NameCtx.Of(a, b));

    private static bool GivenNamesMatch(string a, string b, NameCtx ctx) =>
        GivenNamesMatch(a, b) || (ctx.Cross && CrossGivenNames(a, b, ctx));

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

    /// <summary>
    /// Flags.CrossScriptNames: <paramref name="ar"/> (a token transliterated from Arabic script, which
    /// writes consonants and long vowels and leaves short vowels out) and <paramref name="la"/> (the token
    /// typed in Latin letters, which writes every vowel) are one name. Both are split into consonants and
    /// the vowel letters between them (a medial w or y is the long vowel it writes; a word-initial one is
    /// a consonant), after the token folds, with doubled consonants collapsed, a leading article "al"
    /// set aside and the linking vowel of an "abd" compound read alike (see <see cref="AbdLinks"/>). Then:
    ///   - the consonants are the same, in order (a final h on one side only may be left out), and there
    ///     are at least <paramref name="minConsonants"/> of them;
    ///   - both start with a vowel, or neither does;
    ///   - every long vowel the Arabic side writes (a, y, w) has a vowel of its class in the same gap of
    ///     the Latin side (a: a; y: e, i, y; w: o, u, w), and every long vowel the Latin side writes (a
    ///     doubled vowel, a diphthong, a final i, y, o or u) has one in the Arabic gap. So two names that
    ///     differ in a long vowel stay apart; short vowels are free, since they are what the scripts
    ///     disagree about.
    /// </summary>
    private static bool CrossAligns(string ar, string la, int minConsonants)
    {
        if (ar == la) return true;
        if (CrossAlignsShapes(Canon(CrossPrep(ar)), Canon(CrossPrep(la)), minConsonants, innerH: false)) return true;
        if (!Flags.CrossScriptLetters || Flags.Baseline) return false;
        // Flags.CrossScriptLetters: the letters the two scripts write differently, an h the Latin spelling
        // leaves out, and an Arabic t followed by h that is two letters rather than the interdental.
        string l = CrossLetters(Canon(CrossPrep(la)));
        string arPrep = CrossPrep(ar);
        if (CrossAlignsShapes(CrossLetters(Canon(arPrep)), l, minConsonants, innerH: true)) return true;
        return arPrep.Contains("th", StringComparison.Ordinal)
            && CrossAlignsShapes(CrossLetters(Canon(arPrep.Replace("th", "þ")).Replace("þ", "th")), l, minConsonants, innerH: true);
    }

    /// <summary>Flags.CrossScriptLetters: one spelling of the letters the two scripts write differently — the
    /// d/z class (an emphatic or interdental letter written d, dh or z), v as f, and g and q as k (a gh stays).</summary>
    private static string CrossLetters(string t)
    {
        var sb = new System.Text.StringBuilder(t.Length);
        for (int i = 0; i < t.Length; i++)
        {
            char c = t[i];
            if (c == 'g' && i + 1 < t.Length && t[i + 1] == 'h') { sb.Append("gh"); i++; continue; }
            sb.Append(c switch { 'd' => 'z', 'v' => 'f', 'g' or 'q' => 'k', _ => c });
        }
        return sb.ToString();
    }

    private static bool CrossAlignsShapes(string arForm, string laForm, int minConsonants, bool innerH)
    {
        var (ac, ag) = CrossShape(arForm);
        var (lc, lg) = CrossShape(laForm);
        // An optional final h: a taa marbuta or final heh written as h on one side only.
        if (ac.Count == lc.Count + 1 && ac[^1] == 'h') DropFinal(ac, ag);
        else if (lc.Count == ac.Count + 1 && lc[^1] == 'h') DropFinal(lc, lg);
        // Flags.CrossScriptLetters: one inner h written on one side only (a weak letter the Latin leaves out).
        if (innerH && ac.Count != lc.Count && Math.Abs(ac.Count - lc.Count) == 1 && Math.Min(ac.Count, lc.Count) >= 3)
        {
            var (longC, longG, shortC) = ac.Count > lc.Count ? (ac, ag, lc) : (lc, lg, ac);
            for (int i = 0; i < longC.Count; i++)
            {
                if (longC[i] != 'h') continue;
                bool rest = true;
                for (int k = 0, m = 0; k < longC.Count; k++)
                {
                    if (k == i) continue;
                    if (longC[k] != shortC[m++]) { rest = false; break; }
                }
                if (!rest) continue;
                longG[i] += longG[i + 1];
                longG.RemoveAt(i + 1);
                longC.RemoveAt(i);
                break;
            }
        }
        if (ac.Count != lc.Count || ac.Count < minConsonants) return false;
        for (int i = 0; i < ac.Count; i++) if (ac[i] != lc[i]) return false;
        // The vowel before the first consonant: present on both sides or on neither.
        if ((ag[0].Length > 0) != (lg[0].Length > 0)) return false;
        // Letters after the first of an Arabic leading cluster ("aa", "ay", "aw") are written long vowels.
        if (ag[0].Length > 1 && !Covers(VowelClasses(lg[0]), RequiredClasses(ag[0][1..]))) return false;
        if (!Covers(VowelClasses(ag[0]), LatinLongClasses(lg[0], final: false))) return false;
        for (int g = 1; g < ag.Count; g++)
        {
            bool final = g == ag.Count - 1;
            int required = RequiredClasses(ag[g]);
            // A final Arabic 'a' (taa marbuta, alef maqsura) is any final vowel in Latin.
            if (final && required == ClassA && lg[g].Length > 0) required = 0;
            if (!Covers(VowelClasses(lg[g]), required)) return false;
            if (!Covers(VowelClasses(ag[g]), LatinLongClasses(lg[g], final))) return false;
        }
        return true;

        static void DropFinal(List<char> cons, List<string> gaps)
        {
            cons.RemoveAt(cons.Count - 1);
            gaps[^2] += gaps[^1];
            gaps.RemoveAt(gaps.Count - 1);
        }
        static bool Covers(int have, int need) => (have & need) == need;
    }

    private const int ClassA = 1, ClassI = 2, ClassU = 4;

    /// <summary>The linking vowels an "abd" compound is written with before the name it belongs to.</summary>
    private static readonly string[] AbdLinks = ["ul", "el", "il", "ol", "al", "u"];

    /// <summary>Every linking vowel written after the compound word, longest first.</summary>
    private static readonly string[] AbdPrefixes = [.. AbdLinks.Select(l => "abd" + l)];

    /// <summary>A leading article "al" set aside (a token of five letters or more), and the linking vowel of
    /// an "abd" compound read alike: "abd" followed directly by the name.</summary>
    private static string CrossPrep(string t)
    {
        foreach (var p in AbdPrefixes)
            if (t.Length >= p.Length + 2 && t.StartsWith(p, StringComparison.Ordinal)) return "abd" + t[p.Length..];
        return t.Length >= 5 && t.StartsWith("al", StringComparison.Ordinal) ? t[2..] : t;
    }

    /// <summary>A token as consonants and the vowel letters around them: gaps[0] before the first
    /// consonant, gaps[i] after consonant i-1. a e i o u are vowels; w and y are vowels except at the
    /// start of the token; a consonant repeated with no vowel between is one consonant.</summary>
    private static (List<char> Cons, List<string> Gaps) CrossShape(string t)
    {
        var cons = new List<char>(t.Length);
        var gaps = new List<string>(t.Length + 1);
        var gap = new System.Text.StringBuilder();
        for (int i = 0; i < t.Length; i++)
        {
            char c = t[i];
            bool vowel = c is 'a' or 'e' or 'i' or 'o' or 'u' || (i > 0 && c is 'w' or 'y');
            if (vowel) { gap.Append(c); continue; }
            if (cons.Count > 0 && gap.Length == 0 && cons[^1] == c) continue;   // doubled consonant
            gaps.Add(gap.ToString());
            gap.Clear();
            cons.Add(c);
        }
        gaps.Add(gap.ToString());
        return (cons, gaps);
    }

    /// <summary>The vowel classes a gap holds: a is A; e, i, y are I; o, u, w are U.</summary>
    private static int VowelClasses(string gap)
    {
        int k = 0;
        foreach (char c in gap)
            k |= c switch { 'a' => ClassA, 'e' or 'i' or 'y' => ClassI, 'o' or 'u' or 'w' => ClassU, _ => 0 };
        return k;
    }

    /// <summary>The long vowels an Arabic-side gap writes: a (alef, ain, taa marbuta), y and w. Kurdish e
    /// and o are vowels the other side may write short, so they require nothing.</summary>
    private static int RequiredClasses(string gap)
    {
        int k = 0;
        foreach (char c in gap)
            k |= c switch { 'a' => ClassA, 'y' => ClassI, 'w' => ClassU, _ => 0 };
        return k;
    }

    /// <summary>The long vowels a Latin gap writes: a doubled vowel or a diphthong (aa; ee ii ei ey ie iy ai
    /// ay; oo uu ou ow uw au aw) and, at the end of the token, a final i, y, o or u.</summary>
    private static int LatinLongClasses(string gap, bool final)
    {
        int k = 0;
        for (int i = 0; i + 1 < gap.Length; i++)
        {
            k |= (gap[i], gap[i + 1]) switch
            {
                ('a', 'a') => ClassA,
                ('e', 'e') or ('i', 'i') or ('e', 'i') or ('e', 'y') or ('i', 'e') or ('i', 'y') or ('a', 'i') or ('a', 'y') => ClassI,
                ('o', 'o') or ('u', 'u') or ('o', 'u') or ('o', 'w') or ('u', 'w') or ('a', 'u') or ('a', 'w') => ClassU,
                _ => 0,
            };
        }
        if (final && gap.Length > 0)
            k |= gap[^1] switch { 'i' or 'y' => ClassI, 'o' or 'u' => ClassU, _ => 0 };
        return k;
    }


    // Fold the z/dh/th/zh interdental-emphatic transliteration class (Arabic ظ ذ ض) to a single
    // 'z' so spelling variants of one Arabic letter compare equal. Returns the SAME instance when
    // no digraph is present (the common case), which lets TokensAlign skip recomputing similarity
    // and guarantees byte-identical behavior for every token the fold doesn't touch. Used only in
    // TokensAlign's fuzzy comparison — it never rewrites a stored or displayed name. 'th' here is
    // the ظ/ذ romanization; a genuine 'th'=ث name (Othman, Haitham) only collides if every OTHER
    // letter already matches, which the gold-set + corpus face-validity gates check.
    private static string Canon(string t)
    {
        string c = t.Contains("dh") || t.Contains("th") || t.Contains("zh")
            ? t.Replace("dh", "z").Replace("th", "z").Replace("zh", "z")
            : t;
        // Tenant equivalences (Flags.TokenFolds; empty by default, so the fast path above is the whole
        // story for a tenant that sets none). Same contract: the same instance back when nothing changed.
        var folds = Flags.TokenFolds;
        if (folds.Count == 0 || Flags.Baseline) return c;
        string f = c;
        foreach (var (from, to) in folds)
            if (f.Contains(from, StringComparison.Ordinal)) f = f.Replace(from, to, StringComparison.Ordinal);
        return f == c ? c : f;
    }

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

    /// <summary>True when a normalised name carries one of the engine's organisation tokens (the
    /// business-line test the phone and VIN rules apply). A feed that recognises an organisation by its
    /// own words marks it by appending "company" when this is false.</summary>
    public static bool IsOrganisationName(string normName) => IsLikelyOrg(normName);

    /// <summary>Flags.OrgNameMerge: two names of one organisation. Both organisation-named and
    /// consistent (their organisation tokens align to each other); or the other side's two or more
    /// words are all the organisation's own words (its organisation tokens set aside) — the
    /// organisation written without its legal form, possibly shortened. Either way, a name written
    /// with its words joined counts as the same words, and two forms of one word (a shared stem of five
    /// or more letters covering most of the shorter word: an adjective and a genitive of one country
    /// or town in an inflected language) align.</summary>
    /// <summary>Flags.OrgNameMerge on a shared SOLD VIN: two names of one organisation
    /// (<see cref="OrgNamesConsistent"/>), or an organisation whose name is one word and a record that
    /// carries just that word. On a phone a lone word beside a company may be a contact person's given
    /// name; on the car the company bought it is the company's own name typed without its legal form.</summary>
    private static bool OrgOnSoldVin(string a, string b)
    {
        bool orgA = IsLikelyOrg(a), orgB = IsLikelyOrg(b);
        if (!orgA && !orgB) return false;
        if (OrgNamesConsistent(a, b)) return true;
        if (orgA && orgB) return false;
        var core = NameTokens(orgA ? a : b).Where(t => !OrgTokens.Contains(t)).ToArray();
        var other = NameTokens(orgA ? b : a);
        return core.Length == 1 && other.Length == 1 && core[0].Length >= 4 && TokensAlign(core[0], other[0]);
    }

    private static bool OrgNamesConsistent(string a, string b)
    {
        bool orgA = IsLikelyOrg(a), orgB = IsLikelyOrg(b);
        if (orgA && orgB) return OrgWordsConsistent(a, b) || JoinedAlign(OrgCore(a), OrgCore(b));
        string org = orgA ? a : b, other = orgA ? b : a;
        var otherTokens = NameTokens(other);
        var orgCore = OrgCore(org);
        if (otherTokens.Length < 2)
        {
            // A lone word beside a company is a contact person's given name — unless the tenant's
            // corpus does not attest it as a name part (Flags.StrictNameParts) and it is the company's
            // own one-word name: then it is the company typed without its legal form.
            var core = NameTokens(orgCore);
            return Flags.StrictNameParts && otherTokens.Length == 1 && core.Length == 1 && core[0].Length >= 4
                && !Flags.EstablishedNameParts.Contains(otherTokens[0]) && TokensAlign(core[0], otherTokens[0]);
        }
        return orgCore.Length > 0
            && ((NameTokens(orgCore).Length >= otherTokens.Length && OrgWordsConsistent(other, orgCore)) || JoinedAlign(orgCore, other));

        static string OrgCore(string n) => string.Join(' ', NameTokens(n).Where(t => !OrgTokens.Contains(t)));

        // NameConsistent with organisation word forms: every token of the shorter name aligns
        // (one-to-one under StrictTokenAlignment) by TokensAlign or as a form of the same word.
        static bool OrgWordsConsistent(string x, string y)
        {
            var tx = NameTokens(x);
            var ty = NameTokens(y);
            if (tx.Length == 0 || ty.Length == 0) return false;
            var (small, big) = tx.Length <= ty.Length ? (tx, ty) : (ty, tx);
            if (small.Distinct().Count() < 2) return NameConsistent(x, y);
            if (Flags.StrictTokenAlignment && !Flags.Baseline) return AlignsOneToOne(small, big, WordAlign);
            return small.All(t => big.Any(u => WordAlign(t, u)));
        }

        static bool WordAlign(string t, string u)
        {
            if (TokensAlign(t, u)) return true;
            int p = 0, n = Math.Min(t.Length, u.Length);
            while (p < n && t[p] == u[p]) p++;
            return p >= 5 && p >= 0.7 * n;
        }

        // One name written with and without the spaces between its words ('brim vale' /
        // 'brimvale'): equal, or a typing slip apart (one edit per ten letters), once the spaces are
        // gone. Long strings only; containment is deliberately NOT enough here, and neither is a high
        // Jaro-Winkler: two bodies whose long titles share every word but the place name score above
        // 0.92 on the shared prefix alone.
        static bool JoinedAlign(string x, string y)
        {
            string jx = x.Replace(" ", ""), jy = y.Replace(" ", "");
            if (jx.Length < 6 || jy.Length < 6) return false;
            return jx == jy || Levenshtein(jx, jy) <= Math.Max(1, Math.Min(jx.Length, jy.Length) / 10);
        }
    }

    // Role / shared mailboxes: ONE address that fronts MANY people, so it is contact information but
    // never identity evidence. This is the only guard e-mail needs — and, on the real corpus, the only
    // one that is safe. Both tempting alternatives were measured against a real corpus (2026-07-28)
    // and both destroy true matches:
    //   - demote by DOMAIN: a personal mailbox on the distributor's own domain is one human, and it
    //     is precisely the duplicate pair e-mail is supposed to merge.
    //   - demote by FREQUENCY: the highest-frequency addresses in the tenant are ONE staff member
    //     duplicated hundreds of times (308 goldens under a single distinct name) — the thing that
    //     should merge, not the thing that should be suppressed.
    // What actually separates the two is the LOCAL PART naming a function rather than a person, which
    // is a property of the address itself and needs no corpus statistics to evaluate.
    private static readonly HashSet<string> RoleLocalParts = new(StringComparer.Ordinal)
    {
        "info", "sales", "service", "admin", "office", "reception", "support", "contact",
        "noreply", "no-reply", "donotreply", "help", "enquiries", "inquiries",
        "marketing", "hr", "careers", "jobs", "billing", "accounts", "mail", "test",
    };

    /// <summary>True when a canonical address's local part names a FUNCTION rather than a person.
    /// Public so a host can audit what its corpus would suppress before enabling e-mail matching.</summary>
    public static bool IsRoleEmail(string canonicalEmail)
    {
        int at = canonicalEmail.IndexOf('@');
        return at > 0 && RoleLocalParts.Contains(canonicalEmail[..at]);
    }

    /// <summary>A record's MATCH-ELIGIBLE addresses: canonical form, deduplicated, role mailboxes
    /// dropped. Ingest and survivorship keep every address (the golden surfaces one for
    /// contactability) — this is only the subset allowed to move a pair's score.</summary>
    private static List<string> MatchEmails(RealRecord r)
    {
        if (r.Emails is not { Length: > 0 }) return [];
        var eligible = new List<string>(r.Emails.Length);
        foreach (var raw in r.Emails)
        {
            string canonical = Norm.Email(raw);
            if (canonical.Length > 0 && !IsRoleEmail(canonical) && !eligible.Contains(canonical, StringComparer.Ordinal))
                eligible.Add(canonical);
        }
        return eligible;
    }

    /// <summary>Carries e-mail, but every usable one is a role mailbox — the "suppressed, not absent"
    /// case, which is a different answer to "why didn't these merge" and is traced as such.</summary>
    private static bool HasRoleOnlyEmail(RealRecord r) =>
        r.Emails is { Length: > 0 }
        && r.Emails.Any(e => Norm.Email(e) is { Length: > 0 } c && IsRoleEmail(c))
        && MatchEmails(r).Count == 0;

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
            // E-mail blocks are capped with the identifier keys, not the name keys: a shared mailbox
            // that slipped past the role filter must not explode the candidate set.
            bool capped = key[0] is 'P' or 'S' or 'I' or 'V' or 'E';
            int cap = capped ? phoneBlockCap : nameBlockCap;
            if (list.Count > cap)
            {
                result.SkippedBlocks++;
                result.SkippedBlockRecords += list.Count;
                if (key[0] is 'P' or 'S' or 'I') result.SkippedPhoneBlocks++; // placeholder numbers like '...0000' (54 rows)
                if (isVin) result.SkippedVinBlocks++;                          // fleet / dealer-plate VINs
                if (key[0] == 'E') result.SkippedEmailBlocks++;                // an unlisted shared/role mailbox
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
        // E-mail co-blocks every profile sharing a mailbox. Gated on the per-tenant switch so a tenant
        // with e-mail off emits byte-identical keys to the pre-e-mail engine and pays nothing for the
        // feature existing. Role mailboxes are already filtered out by MatchEmails — without that, one
        // 'info@' address would co-block every customer who ever wrote to the company.
        if (Flags.EmailMatching)
            foreach (var e in MatchEmails(r)) yield return "E:" + e;
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
        public int SkippedBlocks, SkippedPhoneBlocks, SkippedVinBlocks, SkippedEmailBlocks, MaxVinBlock;
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
