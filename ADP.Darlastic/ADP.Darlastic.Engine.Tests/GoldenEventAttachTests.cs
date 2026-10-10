using System.Text.Json;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The rules that put timeline events on golden customers (<see cref="GoldenEventAttach"/>) and the
/// canonical artifact they are staged as (<see cref="GoldenEventsPayload"/>). Synthetic data only:
/// golden ids are small integers, VINs are made-up 17-character strings, and no event carries a
/// person's details — the contract forbids that anyway.
/// </summary>
public class GoldenEventAttachTests
{
    private const string Vin1 = "SYNTHVIN000000001";
    private const string Vin2 = "SYNTHVIN000000002";
    private const string Vin3 = "SYNTHVIN000000003";

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static SourceEvent Visit(string id, DateOnly? date, string src, string rid, string? vin = Vin1) =>
        new(GoldenEventType.ServiceVisit, id, date, vin, src, rid, "7", "Dealer Seven", "Repair order " + id);

    private static SourceEvent Claim(string id, DateOnly? date, string? vin) =>
        new(GoldenEventType.WarrantyClaim, id, date, vin, null, null, "9", "Dealer Nine", "Warranty claim",
            new Dictionary<string, string> { ["claimType"] = "VE" });

    private static SourceEvent Recall(string id, DateOnly? date, string? vin) =>
        new(GoldenEventType.SafetyRecall, id, date, vin, null, null, null, null, "CAMPAIGN-1 Synthetic campaign");

    /// <summary>Profiles: dms|A1 → golden 100, inspection|B1 → golden 200. Everything else unknown.</summary>
    private static long? Profiles(string src, string rid) => (src, rid) switch
    {
        ("dms", "A1") => 100,
        ("inspection", "B1") => 200,
        _ => null,
    };

    /// <summary>
    /// Vin1: owner 100 from 2024-03-01, sold to owner 200 on 2025-06-15 (200 is the current owner).
    /// Vin2: service contacts only — 300 seen 2024-01..2024-12, 400 seen 2025-02..2025-09.
    /// Vin3: a single service contact, 500.
    /// </summary>
    private static Dictionary<string, IReadOnlyList<VinPeriod>> Periods() => new(StringComparer.Ordinal)
    {
        [Vin1] =
        [
            new(100, VinPeriod.OwnerRole, D(2024, 3, 1), D(2025, 6, 15)),
            new(200, VinPeriod.OwnerRole, D(2025, 6, 15), null),
            new(300, VinPeriod.ContactRole, D(2024, 5, 1), D(2024, 5, 1)),
        ],
        [Vin2] =
        [
            new(300, VinPeriod.ContactRole, D(2024, 1, 10), D(2024, 12, 20)),
            new(400, VinPeriod.ContactRole, D(2025, 2, 1), D(2025, 9, 30)),
        ],
        [Vin3] = [new(500, VinPeriod.ContactRole, D(2025, 1, 1), D(2025, 1, 1))],
    };

    private static EventAttachResult Attach(EventAttachOptions? options, params SourceEvent[] events) =>
        GoldenEventAttach.Attach(events, Profiles, Periods(), options);

    private static long[] GoldensOf(EventAttachResult r, string eventId) =>
        r.ByGolden.Where(kv => kv.Value.Any(a => a.Event.EventId == eventId)).Select(kv => kv.Key).OrderBy(x => x).ToArray();

    // ---------------------------------------------------------------- profile-linked

    [Fact]
    public void ProfileLinked_FollowsTheProfilesGolden_RegardlessOfTheVinTimeline()
    {
        // The visit is dated inside golden 200's ownership of Vin1, but it names golden 100's
        // profile: the source's own statement wins over the inference.
        var r = Attach(null, Visit("V1", D(2025, 8, 1), "dms", "A1"));

        Assert.Equal([100L], GoldensOf(r, "V1"));
        Assert.Equal(GoldenEventLink.Profile, r.ByGolden[100].Single().Link);
        Assert.Equal(1, r.Attached[GoldenEventType.ServiceVisit]);
    }

    [Fact]
    public void ProfileLinked_UnknownProfile_IsUnattachedWithReason()
    {
        var r = Attach(null, Visit("V2", D(2025, 8, 1), "dms", "NOPE"));

        Assert.Empty(r.ByGolden);
        Assert.Equal(1, r.Unattached[GoldenEventType.ServiceVisit]);
        Assert.Equal(1, r.UnattachedReasons[$"ServiceVisit/{GoldenEventAttach.NoProfile}"]);
    }

    // ---------------------------------------------------------------- VIN-only, owners

    [Fact]
    public void VinOnly_Dated_GoesToTheOwnerWhosePeriodCoversTheDate()
    {
        var r = Attach(null,
            Claim("C-first-owner", D(2024, 9, 1), Vin1),
            Claim("C-day-before-transfer", D(2025, 6, 14), Vin1),
            Claim("C-transfer-day", D(2025, 6, 15), Vin1),
            Claim("C-second-owner", D(2026, 1, 5), Vin1));

        Assert.Equal([100L], GoldensOf(r, "C-first-owner"));
        Assert.Equal([100L], GoldensOf(r, "C-day-before-transfer"));
        Assert.Equal([200L], GoldensOf(r, "C-transfer-day"));      // half-open: the transfer day is the new owner's
        Assert.Equal([200L], GoldensOf(r, "C-second-owner"));
        Assert.All(r.ByGolden.Values.SelectMany(l => l), a => Assert.Equal(GoldenEventLink.VinOwner, a.Link));
    }

    [Fact]
    public void VinOnly_OwnerLinksBeatServiceContacts()
    {
        // 300 serviced Vin1 on 2024-05-01, inside owner 100's period — the owner gets the claim.
        var r = Attach(new EventAttachOptions { VinContactFallback = true }, Claim("C1", D(2024, 5, 1), Vin1));

        Assert.Equal([100L], GoldensOf(r, "C1"));
    }

    [Fact]
    public void VinOnly_BeforeTheFirstOwnershipPeriod_IsUnattached_NotGivenToTheFirstOwner()
    {
        var r = Attach(null, Claim("C-predelivery", D(2024, 2, 1), Vin1));

        Assert.Empty(r.ByGolden);
        Assert.Equal(1, r.UnattachedReasons[$"WarrantyClaim/{GoldenEventAttach.BeforeOwnership}"]);
    }

    [Fact]
    public void VinOnly_Undated_GoesToTheCurrentOwner()
    {
        var r = Attach(null, Recall("R-open", null, Vin1));

        Assert.Equal([200L], GoldensOf(r, "R-open"));
        Assert.Equal(GoldenEventLink.VinOwner, r.ByGolden[200].Single().Link);
    }

    [Fact]
    public void VinOnly_UnknownOrMissingVin_IsUnattachedWithReason()
    {
        var r = Attach(null, Claim("C-unknown", D(2025, 1, 1), "SYNTHVIN000000099"), Claim("C-novin", D(2025, 1, 1), null));

        Assert.Empty(r.ByGolden);
        Assert.Equal(1, r.UnattachedReasons[$"WarrantyClaim/{GoldenEventAttach.UnknownVin}"]);
        Assert.Equal(1, r.UnattachedReasons[$"WarrantyClaim/{GoldenEventAttach.NoVin}"]);
        Assert.Equal(2, r.TotalUnattached);
    }

    // ---------------------------------------------------------------- VIN-only, service contacts

    [Fact]
    public void VinOnly_ContactsOnly_StaysUnattachedByDefault()
    {
        var r = Attach(null, Claim("C3", D(2025, 1, 1), Vin3));

        Assert.Empty(r.ByGolden);
        Assert.Equal(1, r.UnattachedReasons[$"WarrantyClaim/{GoldenEventAttach.NoOwner}"]);
    }

    [Fact]
    public void VinOnly_ContactFallback_SingleContactTakesTheEvent_DatedOrNot()
    {
        var on = new EventAttachOptions { VinContactFallback = true };
        var r = Attach(on, Claim("C3-dated", D(2026, 3, 1), Vin3), Recall("R3-open", null, Vin3));

        Assert.Equal([500L], GoldensOf(r, "C3-dated"));
        Assert.Equal([500L], GoldensOf(r, "R3-open"));
        Assert.All(r.ByGolden[500], a => Assert.Equal(GoldenEventLink.VinContact, a.Link));
    }

    [Fact]
    public void VinOnly_ContactFallback_SeveralContacts_NeedsExactlyOneCoveringWindow()
    {
        var on = new EventAttachOptions { VinContactFallback = true };
        var r = Attach(on,
            Claim("C2-in-300", D(2024, 6, 1), Vin2),
            Claim("C2-in-400", D(2025, 9, 30), Vin2),     // window end is inclusive for contacts
            Claim("C2-gap", D(2025, 1, 15), Vin2),
            Recall("R2-open", null, Vin2));

        Assert.Equal([300L], GoldensOf(r, "C2-in-300"));
        Assert.Equal([400L], GoldensOf(r, "C2-in-400"));
        Assert.Empty(GoldensOf(r, "C2-gap"));
        Assert.Empty(GoldensOf(r, "R2-open"));
        Assert.Equal(1, r.UnattachedReasons[$"WarrantyClaim/{GoldenEventAttach.AmbiguousContacts}"]);
        Assert.Equal(1, r.UnattachedReasons[$"SafetyRecall/{GoldenEventAttach.AmbiguousContacts}"]);
    }

    // ---------------------------------------------------------------- bookkeeping

    [Fact]
    public void DuplicateTypeAndId_FirstWins_AndIsCounted()
    {
        var r = Attach(null,
            Visit("V1", D(2025, 8, 1), "dms", "A1"),
            Visit("V1", D(2025, 8, 2), "inspection", "B1"),
            Claim("V1", D(2025, 8, 1), Vin1));            // same id, different type: not a duplicate

        Assert.Equal(1, r.Duplicates);
        Assert.Equal([100L], GoldensOf(r, "V1").Where(g => r.ByGolden[g].Any(a => a.Event.Type == GoldenEventType.ServiceVisit)).ToArray());
        Assert.Equal(1, r.Read[GoldenEventType.ServiceVisit]);
        Assert.Equal(1, r.Read[GoldenEventType.WarrantyClaim]);
    }

    [Fact]
    public void NoEvents_ProducesNothing()
    {
        var r = GoldenEventAttach.Attach([], Profiles, Periods());

        Assert.Empty(r.ByGolden);
        Assert.Equal(0, r.TotalRead);
        Assert.Equal(0, r.TotalUnattached);
    }

    // ---------------------------------------------------------------- canonical payload

    [Fact]
    public void Payload_IsCanonical_NewestFirst_UndatedLast_AndIndependentOfInputOrder()
    {
        var events = new List<AttachedEvent>
        {
            new(Recall("R-open", null, Vin1), GoldenEventLink.VinOwner),
            new(Claim("C-old", D(2024, 9, 1), Vin1), GoldenEventLink.VinOwner),
            new(Visit("V-new", D(2026, 2, 1), "dms", "A1"), GoldenEventLink.Profile),
            new(Claim("C-same-day", D(2026, 2, 1), Vin1), GoldenEventLink.VinOwner),
        };

        string a = GoldenEventsPayload.Write(100, events);
        events.Reverse();
        string b = GoldenEventsPayload.Write(100, events);
        Assert.Equal(a, b);

        var (gid, parsed) = GoldenEventsPayload.Parse(a);
        Assert.Equal(100, gid);
        // Same date: type order (ServiceVisit before WarrantyClaim); undated last.
        Assert.Equal(["V-new", "C-same-day", "C-old", "R-open"], parsed.Select(e => e.Id).ToArray());
        Assert.Null(parsed[^1].Date);
        Assert.Equal("dms", parsed[0].SourceSystem);
        Assert.Equal("A1", parsed[0].SourceRecordId);
        Assert.Null(parsed[1].SourceSystem);                  // VIN-only events carry no profile link
        Assert.Equal("VE", parsed[1].Facts["claimType"]);
        Assert.Equal(GoldenEventLink.VinOwner, parsed[1].Link);
    }

    [Fact]
    public void Payload_EscapesAnythingTheSourceHands_AndRoundTrips()
    {
        var tricky = new SourceEvent(GoldenEventType.Inspection, "I\"1", D(2025, 5, 5), Vin2, "inspection", "B1",
            "3", "Dealer \"Three\"\\West", "Line one\nline two\ttabbed — non-ASCII ok",
            new Dictionary<string, string> { ["z"] = "last", ["a"] = "first\r\n", ["status"] = "в работе" });

        string payload = GoldenEventsPayload.Write(200, [new AttachedEvent(tricky, GoldenEventLink.Profile)]);

        using var doc = JsonDocument.Parse(payload);                          // valid JSON
        var e = GoldenEventsPayload.Parse(payload).Events.Single();
        Assert.Equal(tricky.EventId, e.Id);
        Assert.Equal(tricky.Title, e.Title);
        Assert.Equal(tricky.DealerName, e.DealerName);
        Assert.Equal(["a", "status", "z"], e.Facts.Keys.ToArray());          // facts by key, ordinal
        Assert.Equal("first\r\n", e.Facts["a"]);
        Assert.Contains("в работе", payload);                                 // stays readable, not \u-escaped
    }

    [Fact]
    public void Payload_UnknownEventType_IsSkippedOnRead()
    {
        const string payload = """{"goldenId":7,"events":[{"type":"Teleport","id":"x","link":"Profile","title":""},{"type":"Activation","id":"a1","date":"2025-04-11","link":"Profile","src":"activation","rid":"1","title":"Warranty activation"}]}""";

        var (_, events) = GoldenEventsPayload.Parse(payload);

        Assert.Equal(GoldenEventType.Activation, events.Single().Type);
        Assert.Equal(D(2025, 4, 11), events.Single().Date);
    }
}
