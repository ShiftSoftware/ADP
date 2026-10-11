using System.Text.Encodings.Web;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// The engine's match configuration as one value: every per-tenant switch and list on
/// <see cref="Flags"/> that can move a pair's score, its <see cref="MatchFlags"/> or its categories,
/// plus the process's <see cref="Flags.Baseline"/> mode for the record.
///
/// <para><b>Why it is a value.</b> The switches are run-scoped tenant configuration: the batch runner
/// sets them on its own process before it resolves (some come from its settings file, one — the
/// attested name parts — is computed from the very records it resolves). A surface that re-scores a
/// staged pair afterwards, such as the case browser's explanation in a CRM host, runs in another
/// process that never saw them. Scoring there with whatever that process happens to hold explains a
/// pair with rules the resolve did not use, and the explanation contradicts the decision it is meant
/// to explain. So the resolve records the configuration it scored with (the registry's
/// <see cref="ArtifactType"/> artifact, see <see cref="ToJson"/>), and the serving process scores under
/// it (<see cref="Use"/>, or the <c>RealMatcher</c> overloads that take one).</para>
///
/// <para><b>Defaults.</b> <see cref="Defaults"/> is the engine as shipped: every switch off, every list
/// empty. A registry whose resolves only ever ran on the defaults records nothing (see
/// <see cref="WorthRecording"/>), so a tenant that never set a switch is unchanged by construction —
/// it stages no new artifact and its hosts keep reading their own process configuration.</para>
///
/// <para><b>Immutability.</b> Instances never change. The lists are held, not copied, when built from
/// <see cref="Flags"/>; like the flags themselves they must not be mutated once set. Instances read
/// with <see cref="FromJson"/> own their lists.</para>
/// </summary>
public sealed record MatchSettings
{
    /// <summary>The <c>ProjectionState.ArtifactType</c> the resolve records the configuration under.
    /// Not a Cosmos document: the drain leaves it in the registry, where the hosted explanation reads it.</summary>
    public const string ArtifactType = "matchconfig";

    /// <summary>The single key of the <see cref="ArtifactType"/> family: one configuration per registry.</summary>
    public const string ArtifactKey = "effective";

    /// <summary>The newest payload format <see cref="FromJson"/> reads. Raised whenever a field is added
    /// that a reader ignoring it would score differently with — which is every new switch — so an older
    /// reader refuses a newer record instead of misreading it. <see cref="ToJson"/> writes format 2 only
    /// when a format-2 switch is on (<see cref="UsesFormat2"/>); every other configuration keeps its
    /// format-1 bytes, so a registry that recorded one stays zero-delta.</summary>
    public const int FormatVersion = 2;

    private static readonly IReadOnlySet<string> NoNameParts = new HashSet<string>();

    /// <summary>The engine as shipped: every switch off, every list empty, not in baseline mode.</summary>
    public static MatchSettings Defaults { get; } = new();

    /// <summary>The configuration the engine reads at this point of the calling flow: the innermost
    /// <see cref="Use"/> scope, else the process-wide switches on <see cref="Flags"/>.</summary>
    public static MatchSettings Current => Flags.Current;

    /// <summary>The process's <see cref="Flags.Baseline"/> mode when this configuration was taken. A build
    /// mode of the whole process (env CC_BASELINE), recorded so a reader can tell a baseline resolve
    /// apart; <see cref="Use"/> cannot switch it, and the engine always reads the process's own.</summary>
    public bool Baseline { get; init; }

    /// <summary><see cref="Flags.EmailMatching"/>.</summary>
    public bool EmailMatching { get; init; }
    /// <summary><see cref="Flags.StructuredNames"/>.</summary>
    public bool StructuredNames { get; init; }
    /// <summary><see cref="Flags.StrictTokenAlignment"/>.</summary>
    public bool StrictTokenAlignment { get; init; }
    /// <summary><see cref="Flags.NamelessNeedsCorroboration"/>.</summary>
    public bool NamelessNeedsCorroboration { get; init; }
    /// <summary><see cref="Flags.ConflictAwareClustering"/>. Moves clustering, not a pair's score; recorded
    /// so the record is the whole configuration.</summary>
    public bool ConflictAwareClustering { get; init; }
    /// <summary><see cref="Flags.OrgNameMerge"/>.</summary>
    public bool OrgNameMerge { get; init; }
    /// <summary><see cref="Flags.ServicedVinNameFloor"/>.</summary>
    public bool ServicedVinNameFloor { get; init; }
    /// <summary><see cref="Flags.StrictNameParts"/>.</summary>
    public bool StrictNameParts { get; init; }
    /// <summary><see cref="Flags.TokenFolds"/>, in the order they are applied.</summary>
    public IReadOnlyList<(string From, string To)> TokenFolds { get; init; } = [];
    /// <summary><see cref="Flags.EstablishedNameParts"/>.</summary>
    public IReadOnlySet<string> EstablishedNameParts { get; init; } = NoNameParts;
    /// <summary><see cref="Flags.NamePartFolds"/>, in the order they are applied.</summary>
    public IReadOnlyList<(string From, string To)> NamePartFolds { get; init; } = [];
    /// <summary><see cref="Flags.GivenNameSuffixes"/>, in the order they are tried.</summary>
    public IReadOnlyList<string> GivenNameSuffixes { get; init; } = [];
    /// <summary><see cref="Flags.FamilyNameEndings"/>, in the order they are tried.</summary>
    public IReadOnlyList<string> FamilyNameEndings { get; init; } = [];
    /// <summary><see cref="Flags.FamilyNameSexEndings"/>, in the order they are tried.</summary>
    public IReadOnlyList<(string Masculine, string Feminine)> FamilyNameSexEndings { get; init; } = [];
    /// <summary><see cref="Flags.CrossScriptNames"/> (format 2).</summary>
    public bool CrossScriptNames { get; init; }
    /// <summary><see cref="Flags.ExtendedArabicLetters"/> (format 2). Moves the normalised name, which a
    /// host's search normalises with too.</summary>
    public bool ExtendedArabicLetters { get; init; }
    /// <summary><see cref="Flags.NamelessSoldVinNeedsPhone"/> (format 2).</summary>
    public bool NamelessSoldVinNeedsPhone { get; init; }
    /// <summary><see cref="Flags.AnchoredGivenName"/> (format 2).</summary>
    public bool AnchoredGivenName { get; init; }
    /// <summary><see cref="Flags.DistinctGivenNames"/> (format 2).</summary>
    public bool DistinctGivenNames { get; init; }

    /// <summary>True when a switch of format 2 is on: the recorded form then needs format 2.</summary>
    public bool UsesFormat2 => CrossScriptNames || ExtendedArabicLetters || NamelessSoldVinNeedsPhone || AnchoredGivenName || DistinctGivenNames;

    /// <summary>True for the engine as shipped (<see cref="Defaults"/>).</summary>
    public bool IsDefault => Equals(Defaults);

    /// <summary>
    /// Whether a resolve records this configuration: when it is not the engine's defaults, or when the
    /// registry already holds a recorded one (which must then follow the tenant back to the defaults
    /// rather than keep describing rules the resolve no longer uses). A registry that has only ever
    /// resolved on the defaults never gains the artifact.
    /// </summary>
    public bool WorthRecording(bool registryHoldsOne) => registryHoldsOne || !IsDefault;

    /// <summary>
    /// Score under this configuration for the rest of the calling flow, until the returned scope is
    /// disposed: every engine read of <see cref="Flags"/> (scoring, its trace, block keys, name
    /// survivorship, categorisation) sees this configuration instead of the process-wide one. Nothing
    /// process-wide changes, so concurrent requests scoring under different configurations — or a
    /// request beside a process that sets its own — cannot see each other's. The scope follows the
    /// async flow it was opened in (an <see cref="AsyncLocal{T}"/>), including across awaits, and is
    /// restored on dispose; scopes nest. <see cref="Baseline"/> is not switched (see there).
    /// </summary>
    public IDisposable Use()
    {
        var previous = Flags.Scoped;
        Flags.Scoped = this;
        return new Scope(previous);
    }

    private sealed class Scope(MatchSettings? previous) : IDisposable
    {
        private int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) Flags.Scoped = previous;
        }
    }

    /// <summary>The process configuration the environment asks for — what <see cref="Flags"/> starts from,
    /// exactly as each switch always seeded itself (DARLASTIC_* = "1" turns a switch on). The lists have
    /// no environment form and start empty.</summary>
    internal static MatchSettings FromEnvironment()
    {
        static bool On(string name) => Environment.GetEnvironmentVariable(name) == "1";
        return new MatchSettings
        {
            Baseline = On("CC_BASELINE"),
            EmailMatching = On("DARLASTIC_EMAIL_MATCHING"),
            StructuredNames = On("DARLASTIC_STRUCTURED_NAMES"),
            StrictTokenAlignment = On("DARLASTIC_STRICT_TOKEN_ALIGNMENT"),
            NamelessNeedsCorroboration = On("DARLASTIC_NAMELESS_CORROBORATION"),
            ConflictAwareClustering = On("DARLASTIC_CONFLICT_AWARE_CLUSTERING"),
            OrgNameMerge = On("DARLASTIC_ORG_NAME_MERGE"),
            ServicedVinNameFloor = On("DARLASTIC_SERVICED_VIN_NAME_FLOOR"),
            StrictNameParts = On("DARLASTIC_STRICT_NAME_PARTS"),
            CrossScriptNames = On("DARLASTIC_CROSS_SCRIPT_NAMES"),
            ExtendedArabicLetters = On("DARLASTIC_EXTENDED_ARABIC_LETTERS"),
            NamelessSoldVinNeedsPhone = On("DARLASTIC_NAMELESS_SOLD_VIN_PHONE"),
            AnchoredGivenName = On("DARLASTIC_ANCHORED_GIVEN_NAME"),
            DistinctGivenNames = On("DARLASTIC_DISTINCT_GIVEN_NAMES"),
        };
    }

    // ------------------------------------------------------------------ equality

    /// <summary>Two configurations score every pair alike: the same switches and the same lists, the
    /// ordered lists in the same order, the attested name parts as the same set.</summary>
    public bool Equals(MatchSettings? other) =>
        other is not null && (ReferenceEquals(this, other) ||
            (Baseline == other.Baseline && EmailMatching == other.EmailMatching
             && StructuredNames == other.StructuredNames && StrictTokenAlignment == other.StrictTokenAlignment
             && NamelessNeedsCorroboration == other.NamelessNeedsCorroboration
             && ConflictAwareClustering == other.ConflictAwareClustering && OrgNameMerge == other.OrgNameMerge
             && ServicedVinNameFloor == other.ServicedVinNameFloor && StrictNameParts == other.StrictNameParts
             && CrossScriptNames == other.CrossScriptNames && ExtendedArabicLetters == other.ExtendedArabicLetters
             && NamelessSoldVinNeedsPhone == other.NamelessSoldVinNeedsPhone && AnchoredGivenName == other.AnchoredGivenName
             && DistinctGivenNames == other.DistinctGivenNames
             && TokenFolds.SequenceEqual(other.TokenFolds) && NamePartFolds.SequenceEqual(other.NamePartFolds)
             && GivenNameSuffixes.SequenceEqual(other.GivenNameSuffixes, StringComparer.Ordinal)
             && FamilyNameEndings.SequenceEqual(other.FamilyNameEndings, StringComparer.Ordinal)
             && FamilyNameSexEndings.SequenceEqual(other.FamilyNameSexEndings)
             && EstablishedNameParts.Count == other.EstablishedNameParts.Count
             && EstablishedNameParts.SetEquals(other.EstablishedNameParts)));

    public override int GetHashCode()
    {
        var h = new HashCode();
        h.Add(Baseline); h.Add(EmailMatching); h.Add(StructuredNames); h.Add(StrictTokenAlignment);
        h.Add(NamelessNeedsCorroboration); h.Add(ConflictAwareClustering); h.Add(OrgNameMerge);
        h.Add(ServicedVinNameFloor); h.Add(StrictNameParts);
        h.Add(CrossScriptNames); h.Add(ExtendedArabicLetters); h.Add(NamelessSoldVinNeedsPhone); h.Add(AnchoredGivenName);
        h.Add(DistinctGivenNames);
        h.Add(TokenFolds.Count); h.Add(NamePartFolds.Count); h.Add(GivenNameSuffixes.Count);
        h.Add(FamilyNameEndings.Count); h.Add(FamilyNameSexEndings.Count); h.Add(EstablishedNameParts.Count);
        return h.ToHashCode();
    }

    /// <summary>The switches that are on and the size of each list. Never the list contents: the
    /// attested name parts are taken from customer records.</summary>
    public override string ToString()
    {
        var on = new List<string>();
        if (Baseline) on.Add("baseline");
        if (EmailMatching) on.Add("e-mail matching");
        if (StructuredNames) on.Add("structured names");
        if (StrictTokenAlignment) on.Add("strict tokens");
        if (NamelessNeedsCorroboration) on.Add("nameless corroboration");
        if (ConflictAwareClustering) on.Add("conflict-aware clustering");
        if (OrgNameMerge) on.Add("org-name merge");
        if (ServicedVinNameFloor) on.Add("serviced-VIN name floor");
        if (StrictNameParts) on.Add("strict name parts");
        if (CrossScriptNames) on.Add("cross-script names");
        if (ExtendedArabicLetters) on.Add("extended Arabic letters");
        if (NamelessSoldVinNeedsPhone) on.Add("nameless sold-VIN phone check");
        if (AnchoredGivenName) on.Add("anchored given name");
        if (DistinctGivenNames) on.Add("distinct given names");
        void List(string name, int count) { if (count > 0) on.Add($"{name} {count:N0}"); }
        List("token folds", TokenFolds.Count);
        List("name-part folds", NamePartFolds.Count);
        List("given-name suffixes", GivenNameSuffixes.Count);
        List("family-name endings", FamilyNameEndings.Count);
        List("family-name sex endings", FamilyNameSexEndings.Count);
        List("attested name parts", EstablishedNameParts.Count);
        return on.Count == 0 ? "engine defaults" : string.Join(", ", on);
    }

    // ------------------------------------------------------------------ the recorded form

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        // Relaxed, like the other staged payloads: name parts in any script stay readable. Quotes,
        // backslashes and control characters are still escaped, so the output is always valid JSON.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    /// <summary>
    /// The canonical recorded form: fixed member order, ordered lists in their order, the attested name
    /// parts sorted ordinally, one fixed encoder. Equal configurations serialize byte-identically, so the
    /// registry's content hash changes exactly when the configuration does and repeated resolves on one
    /// configuration stay zero-delta.
    /// <code>
    /// {"v":1,"baseline":false,"emailMatching":true,…,"strictNameParts":true,
    ///  "tokenFolds":[["from","to"]],"namePartFolds":[…],"givenNameSuffixes":[…],"familyNameEndings":[…],
    ///  "familyNameSexEndings":[["masculine","feminine"]],"establishedNameParts":[…]}
    /// </code>
    /// </summary>
    public string ToJson()
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, WriterOptions))
        {
            w.WriteStartObject();
            // Format 1 for every configuration format 1 can describe: byte-identical to the records
            // written before format 2 existed, so a registry that holds one stays zero-delta.
            w.WriteNumber("v", UsesFormat2 ? 2 : 1);
            w.WriteBoolean("baseline", Baseline);
            w.WriteBoolean("emailMatching", EmailMatching);
            w.WriteBoolean("structuredNames", StructuredNames);
            w.WriteBoolean("strictTokenAlignment", StrictTokenAlignment);
            w.WriteBoolean("namelessNeedsCorroboration", NamelessNeedsCorroboration);
            w.WriteBoolean("conflictAwareClustering", ConflictAwareClustering);
            w.WriteBoolean("orgNameMerge", OrgNameMerge);
            w.WriteBoolean("servicedVinNameFloor", ServicedVinNameFloor);
            w.WriteBoolean("strictNameParts", StrictNameParts);
            if (UsesFormat2)
            {
                w.WriteBoolean("crossScriptNames", CrossScriptNames);
                w.WriteBoolean("extendedArabicLetters", ExtendedArabicLetters);
                w.WriteBoolean("namelessSoldVinNeedsPhone", NamelessSoldVinNeedsPhone);
                w.WriteBoolean("anchoredGivenName", AnchoredGivenName);
                w.WriteBoolean("distinctGivenNames", DistinctGivenNames);
            }
            Pairs(w, "tokenFolds", TokenFolds);
            Pairs(w, "namePartFolds", NamePartFolds);
            Strings(w, "givenNameSuffixes", GivenNameSuffixes);
            Strings(w, "familyNameEndings", FamilyNameEndings);
            Pairs(w, "familyNameSexEndings", FamilyNameSexEndings);
            Strings(w, "establishedNameParts", EstablishedNameParts.Order(StringComparer.Ordinal));
            w.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());

        static void Strings(Utf8JsonWriter w, string name, IEnumerable<string> values)
        {
            w.WriteStartArray(name);
            foreach (var v in values) w.WriteStringValue(v);
            w.WriteEndArray();
        }

        static void Pairs(Utf8JsonWriter w, string name, IEnumerable<(string, string)> values)
        {
            w.WriteStartArray(name);
            foreach (var (x, y) in values)
            {
                w.WriteStartArray();
                w.WriteStringValue(x);
                w.WriteStringValue(y);
                w.WriteEndArray();
            }
            w.WriteEndArray();
        }
    }

    /// <summary>
    /// Read a recorded configuration. A member that is absent takes its default, and a member this
    /// reader does not know is ignored — but a record of a newer <see cref="FormatVersion"/> is refused:
    /// its new members change scoring, and silently dropping them is the exact failure the record
    /// exists to prevent.
    /// </summary>
    /// <exception cref="FormatException">Not a recorded configuration.</exception>
    /// <exception cref="NotSupportedException">Recorded by a newer engine.</exception>
    public static MatchSettings FromJson(string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex) { throw new FormatException("Not a recorded match configuration: " + ex.Message, ex); }
        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new FormatException("Not a recorded match configuration: not a JSON object.");
            int version = root.TryGetProperty("v", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            if (version < 1) throw new FormatException("Not a recorded match configuration: no format version.");
            if (version > FormatVersion)
                throw new NotSupportedException(
                    $"The match configuration was recorded by a newer engine (format {version}; this engine reads up to {FormatVersion}). " +
                    "Update this host's ADP.Darlastic packages before it explains pairs from this registry.");

            bool Bool(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.True;
            string[] Strings(string name) =>
                root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Array
                    ? [.. e.EnumerateArray().Select(x => x.GetString() ?? "")]
                    : [];
            (string, string)[] Pairs(string name) =>
                root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Array
                    ? [.. e.EnumerateArray().Select(p =>
                        p.ValueKind == JsonValueKind.Array && p.GetArrayLength() == 2
                            ? (p[0].GetString() ?? "", p[1].GetString() ?? "")
                            : throw new FormatException($"Not a recorded match configuration: '{name}' holds a member that is not a pair."))]
                    : [];

            return new MatchSettings
            {
                Baseline = Bool("baseline"),
                EmailMatching = Bool("emailMatching"),
                StructuredNames = Bool("structuredNames"),
                StrictTokenAlignment = Bool("strictTokenAlignment"),
                NamelessNeedsCorroboration = Bool("namelessNeedsCorroboration"),
                ConflictAwareClustering = Bool("conflictAwareClustering"),
                OrgNameMerge = Bool("orgNameMerge"),
                ServicedVinNameFloor = Bool("servicedVinNameFloor"),
                StrictNameParts = Bool("strictNameParts"),
                CrossScriptNames = Bool("crossScriptNames"),
                ExtendedArabicLetters = Bool("extendedArabicLetters"),
                NamelessSoldVinNeedsPhone = Bool("namelessSoldVinNeedsPhone"),
                AnchoredGivenName = Bool("anchoredGivenName"),
                DistinctGivenNames = Bool("distinctGivenNames"),
                TokenFolds = Pairs("tokenFolds"),
                NamePartFolds = Pairs("namePartFolds"),
                GivenNameSuffixes = Strings("givenNameSuffixes"),
                FamilyNameEndings = Strings("familyNameEndings"),
                FamilyNameSexEndings = Pairs("familyNameSexEndings"),
                EstablishedNameParts = new HashSet<string>(Strings("establishedNameParts"), StringComparer.Ordinal),
            };
        }
    }

    /// <summary>
    /// What a registry row of the <see cref="ArtifactType"/> family says: the recorded configuration,
    /// or null when there is none to apply (no row, a tombstone, or a row without its payload). Null
    /// means "score with this process's own configuration", which is what every host did before
    /// configurations were recorded.
    /// </summary>
    public static MatchSettings? FromStored(string? contentHash, string? payload) =>
        contentHash is null || contentHash.TrimEnd() == "TOMBSTONE" || string.IsNullOrEmpty(payload)
            ? null
            : FromJson(payload);
}
