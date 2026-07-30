using System.Text.Json;
using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The contract a hosted steward surface rests on: a case's derivation can be recomputed from what
/// the resolve staged, so the reasoning does not have to be persisted alongside the evidence.
///
/// <para><b>Why this is a test and not a comment.</b> <c>StewardCaseDTO</c> shipped saying the
/// matcher's trace "is computed during a resolve and never persisted, so the steward sees WHAT the
/// two records say and the engine's confidence — not the derivation", and that showing it "needs a
/// new staged artifact family from the engine". That conclusion followed from a packaging
/// constraint (<c>ADP.Darlastic.API</c> could not reference the Engine without inheriting SqlClient
/// and Cosmos), not from missing data. These tests pin the two facts that make the recompute route
/// correct, so a future change that quietly breaks either one fails here rather than in a steward's
/// face as a blank panel or — worse — a plausible trace for the wrong pair.</para>
/// </summary>
public class HostedTraceTests
{
    private static RealRecord Rec(int idx, string src, string id, string name, string? phone = null) =>
        new(idx, src, id, name, name, phone is null ? [] : [phone], [], null, null);

    /// <summary>Exactly what the resolve writes into <c>StewardRecord.Payload</c>.</summary>
    private static string Stage(RealRecord r) => JsonSerializer.Serialize(r);

    [Fact]
    public void StagedPayload_RoundTrips_WithEveryFieldTheScorerReads()
    {
        var original = Rec(0, "sas", "1481", "gais hashem", "7901586508") with
        {
            RawAddress = "AL NAJAF|Najaf",
            NormAddress = "al najaf najaf",
            NormCity = "najaf",
        };

        var revived = JsonSerializer.Deserialize<RealRecord>(Stage(original))!;

        // Scoring a record against its revived self must be indistinguishable from scoring it
        // against itself: any field the serializer drops shows up here as a score difference.
        double self = RealMatcher.Score(original, original with { Idx = 1, SourceRecordId = "1482" });
        double round = RealMatcher.Score(original, revived with { Idx = 1, SourceRecordId = "1482" });
        Assert.Equal(self, round, precision: 10);
    }

    [Fact]
    public void Trace_IsRecomputable_FromStagedPayloadsAlone()
    {
        // Two records as the resolve would have staged them — nothing else available, which is
        // exactly the position a hosted endpoint is in.
        string payloadA = Stage(Rec(0, "sas", "1481", "gais hashem", "7901586508") with { NormAddress = "al najaf najaf", NormCity = "najaf" });
        string payloadB = Stage(Rec(1, "cihan", "902", "gais hashem jasim", "7901586508") with { NormAddress = "al najaf najaf", NormCity = "najaf" });

        var a = JsonSerializer.Deserialize<RealRecord>(payloadA)!;
        var b = JsonSerializer.Deserialize<RealRecord>(payloadB)!;

        var trace = new MatchTrace();
        double score = RealMatcher.Explain(a, b, trace);

        Assert.NotEmpty(trace.Steps);
        // The trace is the scorer's own account of itself: it must both reach a decision and agree
        // with the plain scorer. A trace that narrated a different number than the one the engine
        // acted on would be worse than no trace at all.
        Assert.Equal(RealMatcher.Score(a, b), score, precision: 10);
        Assert.Contains(trace.Steps, s => s.Stage == "decide");
    }

    [Fact]
    public void Categories_AreRecomputable_ForAStagedPair()
    {
        // The catalog stages a pair's category bitmask, but a surface may also want to re-derive it
        // (to show a case the catalog sampled out, or to detect drift against an older run). Same
        // inputs, same rules, no storage.
        var a = Rec(0, "sas", "1", "ahmed ali", "7701234567");
        var b = Rec(1, "cihan", "2", "ahmed ali hassan", "7701234567");

        double s = RealMatcher.Score(a, b, out var flags);
        var cats = CaseCategories.Categorize(a, b, s, flags);

        Assert.NotEqual(CaseCat.None, cats);
        // Band tags are mutually exclusive by construction — a pair cannot be both auto-merged and
        // awaiting a steward, and the browser has been bitten by exactly that contradiction before.
        int bandTags = ((cats & CaseCat.AutoMerged) != 0 ? 1 : 0)
                     + ((cats & CaseCat.StewardBand) != 0 ? 1 : 0)
                     + ((cats & CaseCat.NearMiss) != 0 ? 1 : 0);
        Assert.True(bandTags <= 1, $"pair carried {bandTags} band tags: {cats}");
    }
}
