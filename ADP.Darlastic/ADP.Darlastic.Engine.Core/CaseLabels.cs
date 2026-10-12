namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>A name a person reading a customer list understands, and one line saying what it means.</summary>
public sealed record PlainLabel(string Label, string Description);

/// <summary>
/// The plain-language names of every category, reason and piece of match evidence the engine
/// produces — the ONE place they are defined. Browsing surfaces read these (the hosted case browser
/// is served them with its data) and carry no label map of their own: a second copy next to a UI is
/// how a category comes to mean one thing on screen and another in the engine.
///
/// <para>Written for the people who look after customer records, not for the people who tune the
/// matcher: no scores, thresholds, rule names or enum identifiers. A test fails if any enum value
/// is missing here.</para>
/// </summary>
public static class CaseLabels
{
    /// <summary>Pair categories — what the comparison of two records found.</summary>
    public static IReadOnlyDictionary<CaseCat, PlainLabel> Cases { get; } = new Dictionary<CaseCat, PlainLabel>
    {
        [CaseCat.AutoMerged] = new("Combined automatically",
            "The two records matched closely enough to be combined without a review."),
        [CaseCat.StewardBand] = new("Close match, needs a person",
            "The two records may be the same customer but the match is not certain. Pairs already joined through another record are left out of the review list."),
        [CaseCat.NearMiss] = new("Possible duplicate kept separate",
            "The two records look alike, but not enough to combine them."),
        [CaseCat.ConflictVetoed] = new("Same phone, different names",
            "The records share a phone number but the names clearly differ, so they were kept apart."),
        [CaseCat.AddressRescued] = new("Combined: same phone, first name and area",
            "Same phone and first name, and the addresses agree on the district, so the records were combined."),
        [CaseCat.Increment] = new("Same phone and first name, rest differs",
            "Same phone and first name, but the rest of the name differs; kept apart for now."),
        [CaseCat.Mojibake] = new("Name repaired from unreadable text",
            "One of the names arrived as unreadable characters and was repaired before matching."),
        [CaseCat.Arabizi] = new("Name written with digits for letters",
            "One of the names used digits in place of letters and was converted before matching."),
        [CaseCat.VinSold] = new("Combined by vehicle purchase",
            "Both records bought the same vehicle and the names agree, so they were combined."),
        [CaseCat.VinServiced] = new("Same vehicle serviced",
            "Both records brought the same vehicle in for service. This supports a match but never decides one."),
        [CaseCat.VinTransfer] = new("Vehicle changed owner",
            "The same vehicle appears on both records at different times with a sale in between: two different owners."),
        [CaseCat.VinReview] = new("Same vehicle, similar name",
            "Both records bought the same vehicle and share a first name, but the rest of the name differs; not combined."),
        [CaseCat.OrgLine] = new("Company or fleet",
            "The shared phone belongs to a company, fleet or placeholder name, so a phone match alone does not combine them."),
    };

    /// <summary>Cluster categories — what is notable about a customer built from several records.</summary>
    public static IReadOnlyDictionary<ClusterCat, PlainLabel> Clusters { get; } = new Dictionary<ClusterCat, PlainLabel>
    {
        [ClusterCat.LongestChain] = new("Name completed from a longer record",
            "The name shown is the fullest version found among the customer's records."),
        [ClusterCat.WeakPhoneFallback] = new("Phone from an incomplete number",
            "No complete phone number was found, so the phone shown comes from a shorter, incomplete one."),
        [ClusterCat.CrossDealer] = new("Records from several sources",
            "The customer's records come from two or more systems or dealers."),
        [ClusterCat.Large] = new("Large group (5+ records)",
            "Five or more records were combined into this customer."),
        [ClusterCat.VinBridged] = new("Merged by vehicle purchase record",
            "At least one link between the records is a shared vehicle purchase with agreeing names."),
        [ClusterCat.VinDecisive] = new("Linked only through a vehicle purchase",
            "Without the shared vehicle purchase, these records would not have been combined."),
    };

    /// <summary>Per-customer reasons — the chips on a golden customer row, in the order a list
    /// shows them: what needs a person first, then how the records were combined, then the rest.</summary>
    public static IReadOnlyDictionary<GoldenReason, PlainLabel> Reasons { get; } = new Dictionary<GoldenReason, PlainLabel>
    {
        [GoldenReason.NeedsReview] = new("Needs review",
            "A record here may belong to another customer; a person should decide."),
        [GoldenReason.PossibleDuplicate] = new("Possible duplicate kept separate",
            "Another customer looks similar, but not enough to combine them."),
        [GoldenReason.SamePhoneDifferentNames] = new("Same phone, different names",
            "A record here shares a phone number with a record under a clearly different name."),
        [GoldenReason.SameVehicleSimilarName] = new("Same vehicle, similar name",
            "Another customer with the same first name bought the same vehicle; they were not combined."),
        [GoldenReason.CompanyOrFleet] = new("Company or fleet",
            "A record here looks like a company, fleet or placeholder name rather than a person."),
        [GoldenReason.VehicleChangedOwner] = new("Vehicle changed owner",
            "A vehicle on this customer was also owned by someone else, before or after them."),

        [GoldenReason.SingleRecord] = new("Single record",
            "Only one record is known for this customer."),
        [GoldenReason.MergedByPhoneAndName] = new("Merged automatically: same phone and name",
            "Records were combined because they share a phone number and the names agree."),
        [GoldenReason.MergedByPhoneWithoutName] = new("Merged automatically: same phone, a name missing",
            "Records were combined on a shared phone number while one of them has no name to compare."),
        [GoldenReason.MergedByVehicle] = Clusters[ClusterCat.VinBridged],
        [GoldenReason.MergedByEmail] = new("Merged by e-mail",
            "Records were combined because they share an e-mail address and the names agree."),
        [GoldenReason.MergedByReference] = new("Merged by a link between records",
            "One record points directly at the other, for example a ticket raised for that customer."),
        [GoldenReason.MergedByServicedVehicleAndName] = new("Merged by vehicle service and full name",
            "Records were combined because the same vehicle was serviced under both and the full names agree."),
        [GoldenReason.MergedBySimilarDetails] = new("Merged automatically: matching details",
            "Records were combined because their name and contact details are nearly identical."),
        [GoldenReason.MergedByReviewer] = new("Merged by a reviewer",
            "A person decided that two of these records belong to the same customer."),
        [GoldenReason.LinkedOnlyByVehicle] = Clusters[ClusterCat.VinDecisive],

        [GoldenReason.SeveralSources] = Clusters[ClusterCat.CrossDealer],
        [GoldenReason.LargeGroup] = Clusters[ClusterCat.Large],
        [GoldenReason.NameCompleted] = Clusters[ClusterCat.LongestChain],
        [GoldenReason.PhoneFromShortNumber] = Clusters[ClusterCat.WeakPhoneFallback],
        [GoldenReason.NotInLatestRun] = new("Not checked in the latest run",
            "This customer's records were not part of the latest run because their source was not loaded, so what is shown comes from an earlier run."),
    };

    /// <summary>Match evidence — what fired when two records were compared.</summary>
    public static IReadOnlyDictionary<MatchFlags, PlainLabel> Evidence { get; } = new Dictionary<MatchFlags, PlainLabel>
    {
        [MatchFlags.NamesBoth] = new("Both have a name", "A name comparison was possible."),
        [MatchFlags.PhonesBoth] = new("Both have a phone", "A phone comparison was possible."),
        [MatchFlags.PhoneExact] = new("Same phone number", "Both records carry the same phone number."),
        [MatchFlags.PhoneSuffix] = new("Phone numbers end the same",
            "An incomplete number matches the end of a complete one."),
        [MatchFlags.IdsBoth] = new("Both have an ID number", "An ID number comparison was possible."),
        [MatchFlags.IdEqual] = new("Same ID number", "Both records carry the same ID number."),
        [MatchFlags.IdConflict] = new("Different ID numbers", "The ID numbers differ, which points to two people."),
        [MatchFlags.NameOnlyDamp] = new("Only the name matched",
            "The name is the only thing in common, which is never enough to combine records."),
        [MatchFlags.NameConsistent] = new("Names agree", "Same phone, and every part of the names agrees."),
        [MatchFlags.NameConflictCap] = new("Names clearly differ", "Same phone, but the names have nothing in common."),
        [MatchFlags.AddressBoost] = new("Addresses agree", "The addresses agree down to the district."),
        [MatchFlags.ChainSliceRescue] = new("Same first name and district",
            "Same phone and first name, and the addresses agree on the district."),
        [MatchFlags.DobEqual] = new("Same birth date", "Both records carry the same birth date."),
        [MatchFlags.DobConflict] = new("Different birth dates", "The birth dates differ; noted, but not held against the match."),
        [MatchFlags.PhoneNameMerge] = new("Same phone, overlapping name",
            "Same phone and first name; the rest of the name is a shorter or longer form."),
        [MatchFlags.GenderConflict] = new("Different gender", "One record is male and the other female."),
        [MatchFlags.VinBoth] = new("Both have vehicles", "A vehicle comparison was possible."),
        [MatchFlags.VinSoldOverlap] = new("Bought the same vehicle", "Both records bought the same vehicle at a consistent time."),
        [MatchFlags.VinServiced] = new("Serviced the same vehicle", "Both records brought the same vehicle in for service."),
        [MatchFlags.VinTransfer] = new("Vehicle changed hands",
            "The same vehicle, at different times with a sale in between: two different owners."),
        [MatchFlags.VinSoldMerge] = new("Combined by vehicle purchase",
            "The shared vehicle purchase and agreeing names were enough to combine the records."),
        [MatchFlags.OrgLine] = new("Company or fleet phone",
            "The shared phone belongs to a company or placeholder name."),
        [MatchFlags.SameAsRef] = new("One record points to the other",
            "The source system itself links one record to the other."),
        [MatchFlags.SameAsMerge] = new("Combined by a direct link",
            "The direct link and agreeing names were enough to combine the records."),
        [MatchFlags.EmailsBoth] = new("Both have an e-mail", "An e-mail comparison was possible."),
        [MatchFlags.EmailExact] = new("Same e-mail", "Both records carry the same e-mail address."),
        [MatchFlags.EmailMerge] = new("Combined by e-mail",
            "The shared e-mail address and agreeing names were enough to combine the records."),
        [MatchFlags.EmailRoleOnly] = new("Shared office mailbox ignored",
            "A general mailbox (such as an info address) was left out of the comparison."),
        [MatchFlags.GivenNameConflict] = new("First names differ",
            "Both records give a first name, and they are different names rather than two spellings of one, so they were kept apart."),
        [MatchFlags.FamilyNameConflict] = new("Family names differ",
            "Both records give a family name, and they are different names; a shared first name then makes them namesakes, so they were kept apart."),
        [MatchFlags.NamelessHeld] = new("No name to confirm",
            "One record has no name, and the one thing the records share is not enough on its own to combine them."),
        [MatchFlags.ServicedVinNameMerge] = new("Combined by vehicle service and full name",
            "The same vehicle was serviced under both records and the full names agree, which was enough to combine them."),
    };

    /// <summary>The golden attributes by the type key the engine stages them under.</summary>
    public static IReadOnlyDictionary<string, PlainLabel> Attributes { get; } = new Dictionary<string, PlainLabel>(StringComparer.Ordinal)
    {
        ["full_name"] = new("Name", "The customer's name as shown everywhere."),
        ["phone"] = new("Phone", "The customer's main phone number."),
        ["city"] = new("City", "The city most of the records give."),
        ["national_id"] = new("ID number", "The customer's national ID number."),
        ["email"] = new("E-mail", "The customer's e-mail address."),
    };

    /// <summary>How a golden value was chosen, by the survivorship reason the engine records.</summary>
    public static IReadOnlyDictionary<string, PlainLabel> Survival { get; } = new Dictionary<string, PlainLabel>(StringComparer.Ordinal)
    {
        ["longest-chain"] = new("Fullest version", "The longest form of the name that continues the most common one."),
        ["confidence-recency"] = new("Most common value", "The value most of the customer's records agree on."),
        ["weak-fallback"] = new("Only an incomplete number", "No complete number was available, so the incomplete one is shown."),
        ["source-priority"] = new("Most common value", "The value most of the customer's records agree on."),
    };

    /// <summary>What happened to a scored pair, in words: combined, left for a person, or kept apart.</summary>
    public static PlainLabel Decision(double score) =>
        score >= 0.90 ? new("Combined automatically", "The records matched closely enough to be combined.")
        : score >= Merge.StewardThreshold ? new("Needs a person", "The match is close but not certain; a person decides.")
        : new("Kept separate", "The records were not similar enough to combine.");

    /// <summary>
    /// Every list above as plain nested dictionaries — <c>{"cases": {"NearMiss": {"label", "description"}}, …}</c>
    /// keyed by enum member name — for a surface to serve as-is. Dictionaries rather than records so
    /// the wire form is the same whatever JSON naming policy the serving host uses.
    /// </summary>
    public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> Wire()
    {
        static Dictionary<string, Dictionary<string, string>> Of<TKey>(IReadOnlyDictionary<TKey, PlainLabel> map) where TKey : notnull =>
            map.ToDictionary(kv => kv.Key.ToString()!, kv => new Dictionary<string, string>
            {
                ["label"] = kv.Value.Label,
                ["description"] = kv.Value.Description,
            });

        return new()
        {
            ["cases"] = Of(Cases),
            ["clusters"] = Of(Clusters),
            ["reasons"] = Of(Reasons),
            ["evidence"] = Of(Evidence),
            ["attributes"] = Of(Attributes),
            ["survival"] = Of(Survival),
        };
    }
}
