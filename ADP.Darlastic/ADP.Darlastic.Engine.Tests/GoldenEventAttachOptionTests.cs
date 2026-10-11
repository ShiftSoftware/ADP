using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// The opt-in attachment rules: ambiguous owners left unattached, the pre-delivery window, how they
/// compose with the service-contact fallback, per-type statistics, and the ownership-period builder
/// they share with the resolve. Every rule is checked both ways — with its option on, and with it off
/// (where the result must be exactly what the engine always did). Synthetic data only: golden ids are
/// small integers and VINs are made-up 17-character strings.
/// </summary>
public class GoldenEventAttachOptionTests
{
    private const string Vin = "SYNTHVIN000000011";
    private const string OtherVin = "SYNTHVIN000000012";

    private static readonly EventAttachOptions Off = EventAttachOptions.Default;
    private static readonly EventAttachOptions Ambiguous = new() { AmbiguousOwnersUnattached = true };

    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static SourceEvent VinEvent(string id, DateOnly? date, string? vin = Vin,
        GoldenEventType type = GoldenEventType.ServiceVisit, int? window = null) =>
        new(type, id, date, vin, null, null, "1", "Dealer One", "Synthetic event " + id)
        {
            AttachBeforeFirstSaleWithinDays = window,
        };

    private static SourceEvent Inspection(string id, DateOnly? date, int? window = 60, string? vin = Vin) =>
        VinEvent(id, date, vin, GoldenEventType.Inspection, window);

    private static long? NoProfiles(string src, string rid) => null;

    /// <summary>Owner periods as the resolve builds them, plus optional service contacts.</summary>
    private static Dictionary<string, IReadOnlyList<VinPeriod>> Timeline(
        bool tiesShare, (long Id, DateOnly? FirstSale)[] owners, params VinPeriod[] contacts)
    {
        var periods = OwnershipTimeline.OwnerPeriods(owners, tiesShare)
            .Select(p => new VinPeriod(p.GoldenId, VinPeriod.OwnerRole, p.From, p.To))
            .Concat(contacts)
            .ToList();
        return new(StringComparer.Ordinal) { [Vin] = periods };
    }

    private static EventAttachResult Attach(EventAttachOptions options, Dictionary<string, IReadOnlyList<VinPeriod>> periods,
        params SourceEvent[] events) =>
        GoldenEventAttach.Attach(events, NoProfiles, periods, options);

    private static long[] GoldensOf(EventAttachResult r, string eventId) =>
        r.ByGolden.Where(kv => kv.Value.Any(a => a.Event.EventId == eventId)).Select(kv => kv.Key).OrderBy(x => x).ToArray();

    private static int Reason(EventAttachResult r, GoldenEventType type, string reason) =>
        r.ByType.TryGetValue(type, out var s) ? s.UnattachedByReason.GetValueOrDefault(reason) : 0;

    // ---------------------------------------------------------------- ownership periods

    [Fact]
    public void OwnerPeriods_Default_IsTheLegacyTimeline()
    {
        // Unknown start first, then by first sale, then golden id; each ends where the next starts —
        // so a same-day tie gives the lower id an empty period, and the first of two unknown-start
        // owners stays open-ended.
        var periods = OwnershipTimeline.OwnerPeriods(
        [
            (30, D(2023, 5, 1)),
            (10, null),
            (40, D(2023, 5, 1)),
            (20, null),
            (50, D(2024, 1, 1)),
        ]);

        Assert.Equal(
        [
            (10L, (DateOnly?)null, (DateOnly?)null),
            (20L, null, D(2023, 5, 1)),
            (30L, D(2023, 5, 1), D(2023, 5, 1)),
            (40L, D(2023, 5, 1), D(2024, 1, 1)),
            (50L, D(2024, 1, 1), null),
        ], periods);
    }

    [Fact]
    public void OwnerPeriods_TiesShare_GivesTiedAndUndatedOwnersOneSharedPeriod()
    {
        var periods = OwnershipTimeline.OwnerPeriods(
        [
            (30, D(2023, 5, 1)),
            (10, null),
            (40, D(2023, 5, 1)),
            (20, null),
            (50, D(2024, 1, 1)),
        ], tiesShare: true);

        Assert.Equal(
        [
            (10L, (DateOnly?)null, D(2023, 5, 1)),
            (20L, null, D(2023, 5, 1)),
            (30L, D(2023, 5, 1), D(2024, 1, 1)),
            (40L, D(2023, 5, 1), D(2024, 1, 1)),
            (50L, D(2024, 1, 1), null),
        ], periods);
    }

    [Fact]
    public void OwnerPeriods_WithoutTies_AreTheSameEitherWay()
    {
        (long, DateOnly?)[] owners = [(7, D(2022, 1, 1)), (3, D(2021, 6, 1)), (9, null), (5, D(2025, 2, 2))];

        Assert.Equal(OwnershipTimeline.OwnerPeriods(owners), OwnershipTimeline.OwnerPeriods(owners, tiesShare: true));
    }

    // ---------------------------------------------------------------- ambiguous owners

    [Fact]
    public void TwoCoveringOwners_AttachToBoth_ByDefault_AndStayUnattached_WithTheOption()
    {
        // Overlapping owner periods (co-owners the evidence cannot order).
        var periods = new Dictionary<string, IReadOnlyList<VinPeriod>>(StringComparer.Ordinal)
        {
            [Vin] =
            [
                new(100, VinPeriod.OwnerRole, D(2024, 1, 1), null),
                new(200, VinPeriod.OwnerRole, D(2024, 3, 1), null),
            ],
        };
        var events = new[] { VinEvent("E-both", D(2024, 6, 1)), VinEvent("E-first-only", D(2024, 2, 1)) };

        var off = Attach(Off, periods, events);
        Assert.Equal([100L, 200L], GoldensOf(off, "E-both"));
        Assert.Equal([100L], GoldensOf(off, "E-first-only"));
        Assert.Equal(1, off.MultiGolden);

        var on = Attach(Ambiguous, periods, events);
        Assert.Empty(GoldensOf(on, "E-both"));
        Assert.Equal([100L], GoldensOf(on, "E-first-only"));
        Assert.Equal(0, on.MultiGolden);
        Assert.Equal(1, on.UnattachedReasons[$"ServiceVisit/{GoldenEventAttach.AmbiguousOwners}"]);
        Assert.Equal(1, Reason(on, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousOwners));
    }

    [Fact]
    public void OwnersWithNoSaleDate_LegacyTimeline_AttachToSeveral_ByDefault_NoneWithTheOption()
    {
        // Two owners with no sale date and one dated owner: the legacy timeline leaves the first
        // undated owner open-ended, so a late event lands on it AND the dated owner.
        var legacy = Timeline(false, [(100, null), (200, null), (300, D(2024, 1, 1))]);
        var events = new[] { VinEvent("E-late", D(2025, 1, 1)), VinEvent("E-early", D(2023, 1, 1)), VinEvent("E-open", null) };

        var off = Attach(Off, legacy, events);
        Assert.Equal([100L, 300L], GoldensOf(off, "E-late"));
        Assert.Equal([100L, 200L], GoldensOf(off, "E-early"));
        Assert.Equal([100L, 300L], GoldensOf(off, "E-open"));
        Assert.Equal(3, off.MultiGolden);

        var on = Attach(Ambiguous, legacy, events);
        Assert.Empty(on.ByGolden);
        Assert.Equal(0, on.MultiGolden);
        Assert.Equal(3, Reason(on, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousOwners));
    }

    [Fact]
    public void OwnersWithNoSaleDate_SharedTimeline_OnlyTheirOwnEraIsAmbiguous()
    {
        // As the resolve builds it with the option on: the undated owners share the period before
        // the first dated sale, which itself is unambiguous.
        var shared = Timeline(true, [(100, null), (200, null), (300, D(2024, 1, 1))]);

        var on = Attach(Ambiguous, shared,
            VinEvent("E-late", D(2025, 1, 1)), VinEvent("E-early", D(2023, 1, 1)), VinEvent("E-open", null));

        Assert.Equal([300L], GoldensOf(on, "E-late"));
        Assert.Equal([300L], GoldensOf(on, "E-open"));
        Assert.Empty(GoldensOf(on, "E-early"));
        Assert.Equal(1, Reason(on, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousOwners));
        Assert.Equal(0, on.MultiGolden);
    }

    [Fact]
    public void SameDayTie_LegacyTimeline_GoesToTheHigherId_ByDefault_AndIsAmbiguous_WithTheOption()
    {
        // Two goldens bought the VIN on the same day; a third bought it later.
        var legacy = Timeline(false, [(100, D(2024, 4, 4)), (200, D(2024, 4, 4)), (300, D(2025, 9, 1))]);
        var events = new[] { VinEvent("E-tie-day", D(2024, 4, 4)), VinEvent("E-tie-era", D(2024, 8, 1)), VinEvent("E-after", D(2025, 9, 2)) };

        var off = Attach(Off, legacy, events);
        Assert.Equal([200L], GoldensOf(off, "E-tie-day"));
        Assert.Equal([200L], GoldensOf(off, "E-tie-era"));
        Assert.Equal([300L], GoldensOf(off, "E-after"));
        Assert.Equal(0, off.MultiGolden);

        // The option recognises the tie on the legacy periods too (the lower id's period is empty)…
        var onLegacy = Attach(Ambiguous, legacy, events);
        Assert.Empty(GoldensOf(onLegacy, "E-tie-day"));
        Assert.Empty(GoldensOf(onLegacy, "E-tie-era"));
        Assert.Equal([300L], GoldensOf(onLegacy, "E-after"));
        Assert.Equal(2, Reason(onLegacy, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousOwners));

        // …and on the shared periods the resolve builds with the option on.
        var onShared = Attach(Ambiguous, Timeline(true, [(100, D(2024, 4, 4)), (200, D(2024, 4, 4)), (300, D(2025, 9, 1))]), events);
        Assert.Empty(GoldensOf(onShared, "E-tie-day"));
        Assert.Empty(GoldensOf(onShared, "E-tie-era"));
        Assert.Equal([300L], GoldensOf(onShared, "E-after"));
        Assert.Equal(0, onShared.MultiGolden);
    }

    [Fact]
    public void SameDayTie_OfTheCurrentOwners_LeavesAnUndatedEventUnattached_WithTheOption()
    {
        var legacy = Timeline(false, [(100, D(2024, 4, 4)), (200, D(2024, 4, 4))]);

        Assert.Equal([200L], GoldensOf(Attach(Off, legacy, VinEvent("R-open", null, type: GoldenEventType.SafetyRecall)), "R-open"));

        foreach (var shape in new[] { legacy, Timeline(true, [(100, D(2024, 4, 4)), (200, D(2024, 4, 4))]) })
        {
            var on = Attach(Ambiguous, shape, VinEvent("R-open", null, type: GoldenEventType.SafetyRecall));
            Assert.Empty(on.ByGolden);
            Assert.Equal(1, Reason(on, GoldenEventType.SafetyRecall, GoldenEventAttach.AmbiguousOwners));
        }
    }

    [Fact]
    public void SingleUnambiguousOwner_IsUnaffectedByTheOption()
    {
        var periods = Timeline(true, [(100, D(2023, 1, 1)), (200, D(2024, 1, 1))]);
        var events = new[] { VinEvent("E1", D(2023, 6, 1)), VinEvent("E2", D(2024, 1, 1)), VinEvent("E3", null), VinEvent("E0", D(2022, 1, 1)) };

        var off = Attach(Off, periods, events);
        var on = Attach(Ambiguous, periods, events);

        foreach (var r in new[] { off, on })
        {
            Assert.Equal([100L], GoldensOf(r, "E1"));
            Assert.Equal([200L], GoldensOf(r, "E2"));
            Assert.Equal([200L], GoldensOf(r, "E3"));
            Assert.Empty(GoldensOf(r, "E0"));
            Assert.Equal(1, Reason(r, GoldenEventType.ServiceVisit, GoldenEventAttach.BeforeOwnership));
        }
    }

    // ---------------------------------------------------------------- pre-delivery window

    [Fact]
    public void PreDelivery_InsideTheWindow_GoesToTheFirstSalesOwner_MarkedPreDelivery()
    {
        // Owner 100 buys on 2024-03-01 and sells to 200 a year later.
        var periods = Timeline(false, [(100, D(2024, 3, 1)), (200, D(2025, 3, 1))]);

        var r = Attach(Off, periods,
            Inspection("P-30d", D(2024, 1, 31)),                 // 30 days before the first sale
            Inspection("P-60d", D(2024, 1, 1)),                  // exactly 60 days before
            Inspection("P-61d", D(2023, 12, 31)),                // 61 days before: outside
            Inspection("P-1d", D(2024, 2, 29)));                 // the day before

        Assert.Equal([100L], GoldensOf(r, "P-30d"));
        Assert.Equal([100L], GoldensOf(r, "P-60d"));
        Assert.Equal([100L], GoldensOf(r, "P-1d"));
        Assert.Empty(GoldensOf(r, "P-61d"));
        Assert.All(r.ByGolden[100], a => Assert.Equal(GoldenEventLink.VinPreDelivery, a.Link));
        Assert.Equal(1, Reason(r, GoldenEventType.Inspection, GoldenEventAttach.BeforeOwnership));
        Assert.Equal(3, r.ByType[GoldenEventType.Inspection].AttachedByLink[GoldenEventLink.VinPreDelivery]);
        Assert.Equal(3, r.ByLink[GoldenEventLink.VinPreDelivery]);
    }

    [Fact]
    public void PreDelivery_WithoutADeclaredWindow_StaysBeforeOwnership()
    {
        var periods = Timeline(false, [(100, D(2024, 3, 1))]);

        var r = Attach(Off, periods,
            Inspection("P-none", D(2024, 2, 20), window: null),
            Inspection("P-zero", D(2024, 2, 20), window: 0),
            Inspection("P-negative", D(2024, 2, 20), window: -5));

        Assert.Empty(r.ByGolden);
        Assert.Equal(3, Reason(r, GoldenEventType.Inspection, GoldenEventAttach.BeforeOwnership));
    }

    [Fact]
    public void PreDelivery_NeedsAUniqueFirstOwner_ElseAmbiguousOwners()
    {
        var tie = new[] { (100L, (DateOnly?)D(2024, 3, 1)), (200L, (DateOnly?)D(2024, 3, 1)), (300L, (DateOnly?)D(2025, 1, 1)) };

        foreach (var (options, tiesShare) in new[] { (Off, false), (Ambiguous, false), (Ambiguous, true) })
        {
            var r = Attach(options, Timeline(tiesShare, tie), Inspection("P-tie", D(2024, 2, 1)));
            Assert.Empty(r.ByGolden);
            Assert.Equal(1, Reason(r, GoldenEventType.Inspection, GoldenEventAttach.AmbiguousOwners));
        }
    }

    [Fact]
    public void PreDelivery_GoesToTheFirstSale_NotALaterOneInsideTheWindow()
    {
        // A resale 20 days after the first sale: the inspection 10 days before the first sale
        // belongs to the first buyer's story even though the resale is also within 60 days.
        var r = Attach(Off, Timeline(false, [(100, D(2024, 3, 1)), (200, D(2024, 3, 21))]), Inspection("P1", D(2024, 2, 20)));

        Assert.Equal([100L], GoldensOf(r, "P1"));
    }

    [Fact]
    public void PreDelivery_DoesNotApply_ToCoveredUndatedOrProfileLinkedEvents()
    {
        var periods = Timeline(false, [(100, D(2024, 3, 1)), (200, D(2025, 3, 1))]);
        var profileLinked = new SourceEvent(GoldenEventType.Inspection, "P-profile", D(2024, 2, 20), Vin, "inspections", "R1",
            null, null, "Synthetic inspection") { AttachBeforeFirstSaleWithinDays = 60 };

        var r = Attach(Off, periods,
            Inspection("P-covered", D(2025, 4, 1)),               // inside owner 200's period: ordinary owner link
            Inspection("P-undated", null),                        // undated: the current owner
            profileLinked);                                       // names a profile no golden holds

        Assert.Equal([200L], GoldensOf(r, "P-covered"));
        Assert.Equal([200L], GoldensOf(r, "P-undated"));
        Assert.All(r.ByGolden[200], a => Assert.Equal(GoldenEventLink.VinOwner, a.Link));
        Assert.Equal(1, Reason(r, GoldenEventType.Inspection, GoldenEventAttach.NoProfile));
    }

    [Fact]
    public void PreDelivery_AnOwnerWithNoSaleDate_MeansTheEventIsNotBeforeOwnership()
    {
        // An undated owner reaches back indefinitely: there is no "before the first sale".
        var r = Attach(Off, Timeline(false, [(100, null), (200, D(2024, 3, 1))]), Inspection("P1", D(2024, 2, 20)));

        Assert.Equal([100L], GoldensOf(r, "P1"));
        Assert.Equal(GoldenEventLink.VinOwner, r.ByGolden[100].Single().Link);
    }

    // ---------------------------------------------------------------- composition with the contact fallback

    [Fact]
    public void ContactFallback_NeverRescuesAnEventTheOwnersLeftUnattached()
    {
        var both = new EventAttachOptions { VinContactFallback = true, AmbiguousOwnersUnattached = true };
        var contact = new VinPeriod(900, VinPeriod.ContactRole, D(2020, 1, 1), D(2026, 1, 1));

        // Before ownership: the contact's window covers the date, but the VIN has an owner.
        var before = Attach(both, Timeline(true, [(100, D(2024, 3, 1))], contact), VinEvent("E-before", D(2023, 1, 1)));
        Assert.Empty(before.ByGolden);
        Assert.Equal(1, Reason(before, GoldenEventType.ServiceVisit, GoldenEventAttach.BeforeOwnership));

        // Ambiguous owners: same.
        var tie = Attach(both, Timeline(true, [(100, D(2024, 3, 1)), (200, D(2024, 3, 1))], contact), VinEvent("E-tie", D(2024, 5, 1)));
        Assert.Empty(tie.ByGolden);
        Assert.Equal(1, Reason(tie, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousOwners));
    }

    [Fact]
    public void ContactFallback_StillAppliesToAVinWithNoOwnerLink_WithTheAmbiguityOptionOn()
    {
        var both = new EventAttachOptions { VinContactFallback = true, AmbiguousOwnersUnattached = true };
        var periods = new Dictionary<string, IReadOnlyList<VinPeriod>>(StringComparer.Ordinal)
        {
            [Vin] =
            [
                new(300, VinPeriod.ContactRole, D(2024, 1, 10), D(2024, 12, 20)),
                new(400, VinPeriod.ContactRole, D(2025, 2, 1), D(2025, 9, 30)),
            ],
            [OtherVin] = [new(500, VinPeriod.ContactRole, D(2025, 1, 1), D(2025, 1, 1))],
        };

        var r = GoldenEventAttach.Attach(
        [
            VinEvent("C-in-300", D(2024, 6, 1)),
            VinEvent("C-gap", D(2025, 1, 15)),
            VinEvent("C-single", D(2026, 3, 1), OtherVin),
        ], NoProfiles, periods, both);

        Assert.Equal([300L], GoldensOf(r, "C-in-300"));
        Assert.Equal([500L], GoldensOf(r, "C-single"));
        Assert.Empty(GoldensOf(r, "C-gap"));
        Assert.Equal(1, Reason(r, GoldenEventType.ServiceVisit, GoldenEventAttach.AmbiguousContacts));
        Assert.Equal(2, r.ByType[GoldenEventType.ServiceVisit].AttachedByLink[GoldenEventLink.VinContact]);
    }

    // ---------------------------------------------------------------- dedup and statistics

    [Fact]
    public void Duplicates_FirstWins_PerTypeAndId_AndAreCountedPerType()
    {
        var periods = Timeline(true, [(100, D(2024, 3, 1))]);

        var r = Attach(Ambiguous, periods,
            Inspection("INS-1", D(2024, 2, 20)),                                  // pre-delivery, attached
            Inspection("INS-1", D(2025, 5, 5), window: null),                      // same (type, id): dropped
            VinEvent("INS-1", D(2025, 5, 5), type: GoldenEventType.VehicleSale),   // same id, other type: kept
            VinEvent("S-1", D(2024, 3, 1), type: GoldenEventType.VehicleSale),
            VinEvent("S-1", D(2024, 3, 1), type: GoldenEventType.VehicleSale));

        Assert.Equal(2, r.Duplicates);
        Assert.Equal(1, r.ByType[GoldenEventType.Inspection].Duplicates);
        Assert.Equal(1, r.ByType[GoldenEventType.VehicleSale].Duplicates);
        Assert.Equal(1, r.ByType[GoldenEventType.Inspection].Read);
        Assert.Equal(2, r.ByType[GoldenEventType.VehicleSale].Read);
        var kept = r.ByGolden[100].Single(a => a.Event.Type == GoldenEventType.Inspection);
        Assert.Equal(D(2024, 2, 20), kept.Event.Date);
        Assert.Equal(GoldenEventLink.VinPreDelivery, kept.Link);
        Assert.Equal(3, r.ByGolden[100].Count);
    }

    [Fact]
    public void Statistics_PerType_AddUpAndReachTheRunLog()
    {
        var periods = Timeline(true, [(100, D(2024, 3, 1)), (200, D(2024, 3, 1)), (300, D(2025, 1, 1))]);
        var events = new List<SourceEvent>
        {
            VinEvent("S-tie", D(2024, 3, 1), type: GoldenEventType.VehicleSale),
            VinEvent("S-later", D(2025, 1, 1), type: GoldenEventType.VehicleSale),
            VinEvent("B-1", D(2025, 2, 1), type: GoldenEventType.ServiceBooking),
            VinEvent("B-unknown", D(2025, 2, 1), "SYNTHVIN000000099", GoldenEventType.ServiceBooking),
            new(GoldenEventType.TestDrive, "T-1", D(2025, 2, 2), null, "drives", "X", null, null, "Test drive"),
            new(GoldenEventType.Quotation, "Q-1", D(2025, 2, 3), null, null, null, null, null, "Quotation"),
            new(GoldenEventType.AppRegistration, "A-1", D(2025, 2, 4), null, "app", "Y", null, null, "Registered"),
            VinEvent("B-1", D(2025, 2, 1), type: GoldenEventType.ServiceBooking),
        };

        var r = GoldenEventAttach.Attach(events, (src, rid) => src == "app" ? 300 : null, periods, Ambiguous);

        Assert.Equal(0, r.MultiGolden);
        Assert.Equal(1, r.Duplicates);
        foreach (var (type, s) in r.ByType)
        {
            Assert.Equal(s.Read, s.Attached + s.Unattached);
            Assert.Equal(s.Attached, s.AttachedByLink.Values.Sum());
            Assert.Equal(s.Unattached, s.UnattachedByReason.Values.Sum());
            Assert.Equal(r.Read[type], s.Read);
        }
        Assert.Equal(1, Reason(r, GoldenEventType.VehicleSale, GoldenEventAttach.AmbiguousOwners));
        Assert.Equal(1, Reason(r, GoldenEventType.ServiceBooking, GoldenEventAttach.UnknownVin));
        Assert.Equal(1, Reason(r, GoldenEventType.TestDrive, GoldenEventAttach.NoProfile));
        Assert.Equal(1, Reason(r, GoldenEventType.Quotation, GoldenEventAttach.NoVin));
        Assert.Equal(1, r.ByType[GoldenEventType.AppRegistration].AttachedByLink[GoldenEventLink.Profile]);

        string log = r.Describe();
        foreach (var type in new[] { "VehicleSale", "ServiceBooking", "TestDrive", "Quotation", "AppRegistration" })
            Assert.Contains(type, log);
        Assert.Contains("ambiguous-owners 1", log);
        Assert.Contains("duplicates 1", log);
        Assert.Contains("on several goldens 0", log);
    }

    [Fact]
    public void WithTheOptionOn_NoEventEverLandsOnTwoGoldens()
    {
        // A dense random corpus: many owners per VIN with ties, unknown starts and overlaps.
        var rng = new Random(20261011);
        var periods = new Dictionary<string, IReadOnlyList<VinPeriod>>(StringComparer.Ordinal);
        var events = new List<SourceEvent>();
        for (int v = 0; v < 400; v++)
        {
            string vin = $"SYNTHVIN{v:D9}";
            var owners = Enumerable.Range(0, rng.Next(1, 5))
                .Select(i => ((long)(v * 10 + i), rng.Next(4) == 0 ? (DateOnly?)null : D(2020, 1, 1).AddDays(rng.Next(0, 6) * 200)))
                .ToArray();
            var list = OwnershipTimeline.OwnerPeriods(owners, rng.Next(2) == 0)
                .Select(p => new VinPeriod(p.GoldenId, VinPeriod.OwnerRole, p.From, p.To)).ToList();
            if (rng.Next(3) == 0) list.Add(new VinPeriod(v * 10 + 1, VinPeriod.OwnerRole, D(2021, 1, 1), null));   // overlap
            periods[vin] = list;
            for (int e = 0; e < 10; e++)
                events.Add(VinEvent($"E-{v}-{e}", rng.Next(5) == 0 ? null : D(2019, 1, 1).AddDays(rng.Next(0, 2500)), vin,
                    window: rng.Next(2) == 0 ? 60 : null));
        }

        var off = GoldenEventAttach.Attach(events, NoProfiles, periods, Off);
        var on = GoldenEventAttach.Attach(events, NoProfiles, periods, Ambiguous);

        Assert.True(off.MultiGolden > 0);
        Assert.Equal(0, on.MultiGolden);
        Assert.All(on.ByGolden.Values.SelectMany(l => l).GroupBy(a => a.Event.EventId), g => Assert.Single(g));
        Assert.Equal(off.TotalRead, on.TotalRead);
        Assert.Equal(on.TotalRead, on.TotalAttached + on.TotalUnattached);
    }

    // ---------------------------------------------------------------- new event types in the staged payload

    [Theory]
    [InlineData(GoldenEventType.VehicleSale, "VehicleSale")]
    [InlineData(GoldenEventType.ServiceBooking, "ServiceBooking")]
    [InlineData(GoldenEventType.TestDrive, "TestDrive")]
    [InlineData(GoldenEventType.Quotation, "Quotation")]
    [InlineData(GoldenEventType.AppRegistration, "AppRegistration")]
    public void Payload_NewEventTypes_RoundTripByName(GoldenEventType type, string name)
    {
        var e = new SourceEvent(type, "X-1", D(2025, 5, 5), Vin, null, null, "1", null, "Synthetic");

        string payload = GoldenEventsPayload.Write(7, [new AttachedEvent(e, GoldenEventLink.VinPreDelivery)]);

        Assert.Contains($"\"type\":\"{name}\"", payload);
        Assert.Contains("\"link\":\"VinPreDelivery\"", payload);
        var back = GoldenEventsPayload.Parse(payload).Events.Single();
        Assert.Equal(type, back.Type);
        Assert.Equal(GoldenEventLink.VinPreDelivery, back.Link);
    }

    [Fact]
    public void Payload_NewTypesSortAfterTheOriginalOnes_OnTheSameDay()
    {
        var day = D(2025, 5, 5);
        var events = Enum.GetValues<GoldenEventType>()
            .Select(t => new AttachedEvent(new SourceEvent(t, "same", day, Vin, null, null, null, null, t.ToString()), GoldenEventLink.VinOwner))
            .Reverse()
            .ToList();

        var parsed = GoldenEventsPayload.Parse(GoldenEventsPayload.Write(1, events)).Events;

        Assert.Equal(Enum.GetValues<GoldenEventType>(), parsed.Select(e => e.Type).ToArray());
    }
}
