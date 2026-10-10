using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The recorded match configuration (<see cref="MatchSettings"/>): what a resolve stages in the
/// registry so a host that re-scores one of its pairs in another process — the hosted case browser —
/// reaches the decision the resolve staged. Covers the recorded form (round trip, canonical bytes,
/// refusal of a newer format), the host side (scoring under a loaded configuration reproduces a
/// resolve's decision where the defaults do not; no record leaves the host on its own configuration),
/// the per-resolve cache, and the scope's isolation. Names are invented; phones and e-mail addresses
/// are synthetic. Runs in the engine-flags collection: the "runner" half sets process-wide flags.
/// </summary>
[Collection(EngineFlagsCollection.Name)]
public class MatchSettingsTests
{
    private const string Phone = "100200300";

    /// <summary>A configuration with every switch on and every list filled — the shape a tenant
    /// runner produces, with invented values.</summary>
    private static MatchSettings Everything() => new()
    {
        EmailMatching = true,
        StructuredNames = true,
        StrictTokenAlignment = true,
        NamelessNeedsCorroboration = true,
        ConflictAwareClustering = true,
        OrgNameMerge = true,
        ServicedVinNameFloor = true,
        StrictNameParts = true,
        TokenFolds = [("qx", "k"), ("vv", "w")],
        EstablishedNameParts = new HashSet<string> { "quellmar", "dorvan", "dosvan" },
        NamePartFolds = [("d", "t"), ("zv", "s")],
        GivenNameSuffixes = ["jon", "bek"],
        FamilyNameEndings = ["ovski", "ov"],
        FamilyNameSexEndings = [("ov", "ova"), ("in", "ina")],
    };

    /// <summary>Sets the process-wide flags the way a batch runner does, and restores them.</summary>
    private sealed class Runner : IDisposable
    {
        private readonly MatchSettings before = Flags.Current;

        public Runner(MatchSettings s) => Apply(s);

        public void Dispose() => Apply(before);

        private static void Apply(MatchSettings s)
        {
            Flags.EmailMatching = s.EmailMatching;
            Flags.StructuredNames = s.StructuredNames;
            Flags.StrictTokenAlignment = s.StrictTokenAlignment;
            Flags.NamelessNeedsCorroboration = s.NamelessNeedsCorroboration;
            Flags.ConflictAwareClustering = s.ConflictAwareClustering;
            Flags.OrgNameMerge = s.OrgNameMerge;
            Flags.ServicedVinNameFloor = s.ServicedVinNameFloor;
            Flags.StrictNameParts = s.StrictNameParts;
            Flags.TokenFolds = s.TokenFolds;
            Flags.EstablishedNameParts = s.EstablishedNameParts;
            Flags.NamePartFolds = s.NamePartFolds;
            Flags.GivenNameSuffixes = s.GivenNameSuffixes;
            Flags.FamilyNameEndings = s.FamilyNameEndings;
            Flags.FamilyNameSexEndings = s.FamilyNameSexEndings;
        }
    }

    private static RealRecord Rec(int idx, string id, string name, string? phone = Phone,
        string? given = null, string? family = null, string[]? emails = null) =>
        new(idx, "src", id, name, name, phone is null ? [] : [phone], [], null, null,
            Emails: emails, GivenName: given, FamilyName: family);

    private static string Decision(double s) => s >= 0.90 ? "auto-merge" : s >= 0.80 ? "steward-queue" : "kept-separate";

    // ---------------------------------------------------------------- defaults and recording

    [Fact]
    public void Defaults_AreTheEngineAsShipped_AndAreNeverRecordedOnTheirOwn()
    {
        Assert.True(MatchSettings.Defaults.IsDefault);
        Assert.True(new MatchSettings().IsDefault);
        Assert.False(MatchSettings.Defaults.WorthRecording(registryHoldsOne: false));
        // Once a registry holds a configuration, it follows the tenant back to the defaults.
        Assert.True(MatchSettings.Defaults.WorthRecording(registryHoldsOne: true));
        Assert.True((MatchSettings.Defaults with { StructuredNames = true }).WorthRecording(registryHoldsOne: false));
        Assert.True((MatchSettings.Defaults with { GivenNameSuffixes = ["bek"] }).WorthRecording(registryHoldsOne: false));
        Assert.Equal("engine defaults", MatchSettings.Defaults.ToString());
    }

    [Fact]
    public void Current_IsWhatTheProcessWideFlagsSay()
    {
        var s = Everything();
        using (new Runner(s))
        {
            Assert.Equal(s, MatchSettings.Current);
            Assert.False(MatchSettings.Current.IsDefault);
        }
    }

    // ---------------------------------------------------------------- the recorded form

    [Fact]
    public void RecordedForm_RoundTrips_ByteForByte()
    {
        var s = Everything();
        string json = s.ToJson();
        var back = MatchSettings.FromJson(json);

        Assert.Equal(s, back);
        Assert.Equal(json, back.ToJson());
        Assert.Equal(s.GetHashCode(), back.GetHashCode());
        // Every list survives in its order; the set as the same set.
        Assert.Equal(s.TokenFolds, back.TokenFolds);
        Assert.Equal(s.NamePartFolds, back.NamePartFolds);
        Assert.Equal(s.GivenNameSuffixes, back.GivenNameSuffixes);
        Assert.Equal(s.FamilyNameEndings, back.FamilyNameEndings);
        Assert.Equal(s.FamilyNameSexEndings, back.FamilyNameSexEndings);
        Assert.True(back.EstablishedNameParts.SetEquals(s.EstablishedNameParts));
        Assert.True(MatchSettings.FromJson(MatchSettings.Defaults.ToJson()).IsDefault);
    }

    [Fact]
    public void RecordedForm_IsCanonical_SoTheSameConfigurationHashesTheSame()
    {
        // The attested set's insertion order is not part of the configuration; fold order is.
        var a = Everything() with { EstablishedNameParts = new HashSet<string> { "dosvan", "quellmar", "dorvan" } };
        var b = Everything() with { EstablishedNameParts = new HashSet<string> { "quellmar", "dorvan", "dosvan" } };
        Assert.Equal(a.ToJson(), b.ToJson());
        Assert.Equal(a, b);

        var reordered = Everything() with { NamePartFolds = [("zv", "s"), ("d", "t")] };
        Assert.NotEqual(Everything().ToJson(), reordered.ToJson());
        Assert.NotEqual(Everything(), reordered);

        var oneMorePart = Everything() with { EstablishedNameParts = new HashSet<string> { "quellmar", "dorvan", "dosvan", "torvin" } };
        Assert.NotEqual(Everything().ToJson(), oneMorePart.ToJson());
    }

    [Fact]
    public void RecordedForm_ReadsAbsentMembersAsDefaults_AndIgnoresUnknownOnes()
    {
        var s = MatchSettings.FromJson("""{"v":1,"structuredNames":true,"somethingLater":[1,2]}""");
        Assert.Equal(MatchSettings.Defaults with { StructuredNames = true }, s);
    }

    [Fact]
    public void RecordedForm_FromANewerEngine_IsRefused_NotMisread()
    {
        Assert.Throws<NotSupportedException>(() => MatchSettings.FromJson("""{"v":2,"structuredNames":true}"""));
        Assert.Throws<FormatException>(() => MatchSettings.FromJson("""{"structuredNames":true}"""));
        Assert.Throws<FormatException>(() => MatchSettings.FromJson("not json"));
        Assert.Throws<FormatException>(() => MatchSettings.FromJson("""{"v":1,"tokenFolds":[["a"]]}"""));
    }

    [Fact]
    public void FromStored_NoRowATombstoneOrNoPayload_MeansNothingToApply()
    {
        string json = Everything().ToJson();
        Assert.Null(MatchSettings.FromStored(null, null));
        Assert.Null(MatchSettings.FromStored("TOMBSTONE", null));
        Assert.Null(MatchSettings.FromStored("TOMBSTONE      ", json));
        Assert.Null(MatchSettings.FromStored("abc", null));
        Assert.Equal(Everything(), MatchSettings.FromStored("abc", json));
    }

    // ---------------------------------------------------------------- the host reproduces the resolve

    /// <summary>
    /// The defect this record exists for, end to end on a synthetic pair: a runner resolves with a
    /// tenant's rules — including an attested name-part set it computed from its records — and a host
    /// process that only has the engine defaults explains the staged pair. With the configuration the
    /// walk recorded, the host reproduces the staged score, flags, categories and decision and shows
    /// the tenant's rule in its trace; with its own defaults it reaches the opposite decision.
    /// </summary>
    [Fact]
    public void HostScorer_UnderTheRecordedConfiguration_ReproducesTheResolveDecision_WhereDefaultsDoNot()
    {
        // Two siblings on one family phone: the given names are two attested names one consonant apart.
        var records = new List<RealRecord>
        {
            Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar"),
            Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar"),
        };
        var tenant = MatchSettings.Defaults with
        {
            StructuredNames = true,
            StrictNameParts = true,
            EstablishedNameParts = new HashSet<string> { "dorvan", "dosvan", "quellmar" },
        };

        // ---- the runner: resolve with the tenant's rules, stage the pair and the configuration
        Merge.Result merged;
        string recorded;
        using (new Runner(tenant))
        {
            merged = Merge.ClusterFromBlocks(records, RealMatcher.BuildBlocks(records));
            Assert.NotNull(merged.Settings);
            Assert.Equal(tenant, merged.Settings);
            Assert.True(merged.Settings!.WorthRecording(registryHoldsOne: false));
            recorded = merged.Settings.ToJson();
        }
        var staged = Assert.Single(merged.Catalog);
        Assert.Equal(2, merged.IdentityCount);                       // not merged
        Assert.True((staged.Flags & MatchFlags.GivenNameConflict) != 0);
        Assert.Equal("kept-separate", Decision(staged.Score));

        // ---- the host: a process on the engine defaults (whatever this test process's environment says)
        var a = records[0];
        var b = records[1];
        using var host = MatchSettings.Defaults.Use();
        var hostOwn = MatchSettings.Current;

        var defaultTrace = new MatchTrace();
        double withDefaults = RealMatcher.Explain(a, b, defaultTrace);
        Assert.Equal("auto-merge", Decision(withDefaults));          // the contradiction the host used to serve
        Assert.DoesNotContain(defaultTrace.Steps, s => s.Title == "First names differ");

        var loaded = MatchSettings.FromJson(recorded);
        var trace = new MatchTrace();
        double live = RealMatcher.Explain(a, b, trace, loaded);
        double scored = RealMatcher.Score(a, b, out var liveFlags, loaded);

        Assert.Equal(staged.Score, (float)live);
        Assert.Equal(live, scored);
        Assert.Equal(Decision(staged.Score), Decision(live));
        Assert.Equal(staged.Flags, liveFlags);
        Assert.Contains(trace.Steps, s => s.Title == "First names differ");

        // The same through a scope, the way a hosted request applies it: categories agree too.
        using (loaded.Use())
        {
            double s = RealMatcher.Score(a, b, out var f);
            Assert.Equal(staged.Cats, CaseCategories.Categorize(a, b, s, f));
        }
        Assert.Same(hostOwn, MatchSettings.Current);                 // the host's own configuration is back
    }

    [Fact]
    public void TheSwitchesAlone_WithoutTheAttestedNameParts_DoNotReproduceTheResolve()
    {
        // The attested set is computed by the runner from its records; a host that copies the switches
        // from its own environment but not the set still explains the pair differently.
        var a = Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar");
        var b = Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar");
        var tenant = MatchSettings.Defaults with
        {
            StructuredNames = true,
            StrictNameParts = true,
            EstablishedNameParts = new HashSet<string> { "dorvan", "dosvan" },
        };
        var switchesOnly = tenant with { EstablishedNameParts = new HashSet<string>() };

        Assert.Equal("kept-separate", Decision(RealMatcher.Score(a, b, out _, tenant)));
        Assert.Equal("auto-merge", Decision(RealMatcher.Score(a, b, out _, switchesOnly)));
    }

    [Fact]
    public void Scope_CoversBlockKeys_NotOnlyTheScore()
    {
        var r = Rec(0, "a", "dorvan quellmar", emails: ["dorvan.q@example.test"]);
        using var host = MatchSettings.Defaults.Use();
        Assert.DoesNotContain(RealMatcher.BlockKeysOf(r), k => k.StartsWith("E:", StringComparison.Ordinal));
        using (new MatchSettings { EmailMatching = true }.Use())
            Assert.Contains(RealMatcher.BlockKeysOf(r), k => k.StartsWith("E:", StringComparison.Ordinal));
        Assert.DoesNotContain(RealMatcher.BlockKeysOf(r), k => k.StartsWith("E:", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- no record: the host is unchanged

    [Fact]
    public async Task NoRecordedConfiguration_LeavesTheHostOnItsOwnConfiguration()
    {
        var cache = new RecordedMatchSettings();
        var settings = await cache.GetAsync(_ => Task.FromResult(7), _ => Task.FromResult<(string?, string?)>((null, null)),
            TestContext.Current.CancellationToken);
        Assert.Null(settings);
        Assert.Equal(7, cache.RunId);

        // What a host does with null: nothing — the request scores on the process configuration.
        var a = Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar");
        var b = Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar");
        double before = RealMatcher.Score(a, b, out var fBefore);
        double after;
        MatchFlags fAfter;
        using (settings?.Use()) after = RealMatcher.Score(a, b, out fAfter);
        Assert.Equal(before, after);
        Assert.Equal(fBefore, fAfter);
        Assert.Equal(before, RealMatcher.Score(a, b, out _, MatchSettings.Current));
    }

    // ---------------------------------------------------------------- the per-resolve cache

    [Fact]
    public async Task Cache_ReadsOncePerFinishedRun_AndFollowsTheRecord()
    {
        var cache = new RecordedMatchSettings();
        var ct = TestContext.Current.CancellationToken;
        int run = 5, reads = 0;
        (string? Hash, string? Payload) row = ("h1", Everything().ToJson());
        Task<int> Latest(CancellationToken _) => Task.FromResult(run);
        Task<(string?, string?)> Read(CancellationToken _) { reads++; return Task.FromResult<(string?, string?)>(row); }

        var first = await cache.GetAsync(Latest, Read, ct);
        Assert.Equal(Everything(), first);
        Assert.Same(first, await cache.GetAsync(Latest, Read, ct));
        Assert.Equal(1, reads);                                       // same run: no re-read

        run = 6;                                                      // a zero-delta re-run: same hash
        Assert.Same(first, await cache.GetAsync(Latest, Read, ct));
        Assert.Equal(2, reads);

        run = 7;                                                      // a run that changed the configuration
        row = ("h2", (Everything() with { OrgNameMerge = false }).ToJson());
        var changed = await cache.GetAsync(Latest, Read, ct);
        Assert.False(changed!.OrgNameMerge);

        run = 3;                                                      // a registry restored from a backup
        row = (null, null);
        Assert.Null(await cache.GetAsync(Latest, Read, ct));
        Assert.Equal(4, reads);
    }

    [Fact]
    public async Task Cache_ManyConcurrentRequests_ReadTheChangedRunOnce()
    {
        var cache = new RecordedMatchSettings();
        var ct = TestContext.Current.CancellationToken;
        int reads = 0;
        string json = Everything().ToJson();
        async Task<(string?, string?)> Read(CancellationToken _)
        {
            Interlocked.Increment(ref reads);
            await Task.Delay(20, ct);
            return ("h", json);
        }
        var all = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => cache.GetAsync(_ => Task.FromResult(9), Read, ct), ct)));
        Assert.Equal(1, reads);
        Assert.All(all, s => Assert.Same(all[0], s));
    }

    // ---------------------------------------------------------------- the scope

    [Fact]
    public async Task Scopes_OnConcurrentFlows_DoNotSeeEachOther()
    {
        var a = Rec(0, "a", "dorvan quellmar", given: "dorvan", family: "quellmar");
        var b = Rec(1, "b", "dosvan quellmar", given: "dosvan", family: "quellmar");
        var tenant = MatchSettings.Defaults with
        {
            StructuredNames = true,
            StrictNameParts = true,
            EstablishedNameParts = new HashSet<string> { "dorvan", "dosvan" },
        };
        var processWide = Flags.Current;
        var ct = TestContext.Current.CancellationToken;
        double expectTenant = RealMatcher.Score(a, b, out _, tenant);
        double expectDefault = RealMatcher.Score(a, b, out _, MatchSettings.Defaults);
        Assert.NotEqual(Decision(expectTenant), Decision(expectDefault));

        var results = await Task.WhenAll(Enumerable.Range(0, 64).Select(i => Task.Run(async () =>
        {
            var mine = i % 2 == 0 ? tenant : MatchSettings.Defaults;
            using (mine.Use())
            {
                await Task.Yield();                                   // the scope follows the flow across awaits
                double s1 = RealMatcher.Score(a, b, out _);
                await Task.Delay(1, ct);
                double s2 = RealMatcher.Score(a, b, out _);
                return (i, s1, s2);
            }
        }, ct)));
        foreach (var (i, s1, s2) in results)
        {
            double want = i % 2 == 0 ? expectTenant : expectDefault;
            Assert.Equal(want, s1);
            Assert.Equal(want, s2);
        }
        Assert.Same(processWide, Flags.Current);
    }

    [Fact]
    public void Scopes_Nest_AndRestoreOnDispose()
    {
        var outer = MatchSettings.Defaults with { StructuredNames = true };
        var inner = MatchSettings.Defaults with { OrgNameMerge = true };
        var processWide = Flags.Current;
        using (outer.Use())
        {
            Assert.True(Flags.StructuredNames);
            using (inner.Use())
            {
                Assert.False(Flags.StructuredNames);
                Assert.True(Flags.OrgNameMerge);
            }
            Assert.True(Flags.StructuredNames);
            Assert.False(Flags.OrgNameMerge);
        }
        Assert.Same(processWide, Flags.Current);
        var scope = outer.Use();
        scope.Dispose();
        scope.Dispose();                                              // a second dispose changes nothing
        Assert.Same(processWide, Flags.Current);
    }

    [Fact]
    public void SettingAFlag_InsideAScope_ChangesTheProcessWideValue_NotTheScope()
    {
        bool before = Flags.OrgNameMerge;
        try
        {
            using (MatchSettings.Defaults.Use())
            {
                Flags.OrgNameMerge = !before;
                Assert.False(Flags.OrgNameMerge);                    // the scope still decides here
            }
            Assert.Equal(!before, Flags.OrgNameMerge);               // the process-wide value moved
        }
        finally { Flags.OrgNameMerge = before; }
    }
}
