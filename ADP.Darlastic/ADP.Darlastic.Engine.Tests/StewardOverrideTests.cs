using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// Sticky attribute overrides: a value a person set for a customer stands over what survivorship would choose,
/// travels with its anchor record through a merge, and yields to a newer override. Synthetic values.
/// </summary>
public class StewardOverrideTests
{
    private static RealRecord Rec(int idx, string src, string id, string name, string? phone = null) =>
        new(idx, src, id, name, name, phone is null ? [] : [phone], [], null, null);

    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static string? Value(List<Merge.GoldenAttr> attrs, string type) => attrs.SingleOrDefault(a => a.AttrType == type)?.Value;

    [Fact]
    public void Without_overrides_survivorship_stands_and_two_phones_tie_on_the_lower_one()
    {
        var members = new List<RealRecord> { Rec(0, "dms", "a", "ahmed ali", "7709998888"), Rec(1, "crm", "b", "ahmed ali hassan", "7701112222") };
        var survived = Merge.SurviveGolden(members);

        Assert.Same(survived, Merge.ApplyOverrides(survived, StewardOverrides.None.For(members)));
        Assert.Equal("7701112222", Value(survived, GoldenAttribute.Phone));
    }

    [Fact]
    public void An_override_replaces_the_survived_value_and_adds_one_survivorship_had_not()
    {
        var members = new List<RealRecord> { Rec(0, "dms", "a", "ahmed ali", "7709998888"), Rec(1, "crm", "b", "ahmed ali hassan", "7701112222") };
        var overrides = new StewardOverrides();
        overrides.Add("dms", "a", GoldenAttribute.Phone, "7709998888", T0, 1);
        overrides.Add("dms", "a", GoldenAttribute.City, "Erbil", T0, 2);

        var golden = Merge.ApplyOverrides(Merge.SurviveGolden(members), overrides.For(members));

        Assert.Equal("7709998888", Value(golden, GoldenAttribute.Phone));
        Assert.Equal(Merge.WonByOverride, golden.Single(a => a.AttrType == GoldenAttribute.Phone).WonBy);
        Assert.Equal("Erbil", Value(golden, GoldenAttribute.City));
        // What was not overridden is survivorship's own.
        Assert.Equal(Value(Merge.SurviveGolden(members), GoldenAttribute.FullName), Value(golden, GoldenAttribute.FullName));
        Assert.NotEqual(Merge.WonByOverride, golden.Single(a => a.AttrType == GoldenAttribute.FullName).WonBy);
    }

    [Fact]
    public void An_override_follows_its_record_and_applies_to_no_other_customer()
    {
        var mine = new List<RealRecord> { Rec(0, "dms", "a", "ahmed ali", "7709998888") };
        var other = new List<RealRecord> { Rec(1, "crm", "b", "sara mahmood", "7701112222") };
        var overrides = new StewardOverrides();
        overrides.Add("dms", "a", GoldenAttribute.FullName, "Ahmed Ali Hassan", T0, 1);

        Assert.Equal("Ahmed Ali Hassan", Value(Merge.ApplyOverrides(Merge.SurviveGolden(mine), overrides.For(mine)), GoldenAttribute.FullName));
        Assert.Empty(overrides.For(other));
        // Merged with another customer: the override is on the merged customer, whichever number it keeps.
        List<RealRecord> merged = [.. other, .. mine];
        Assert.Equal("Ahmed Ali Hassan", Value(Merge.ApplyOverrides(Merge.SurviveGolden(merged), overrides.For(merged)), GoldenAttribute.FullName));
    }

    [Fact]
    public void The_newest_override_wins_when_a_customers_records_carry_several()
    {
        var members = new List<RealRecord> { Rec(0, "dms", "a", "ahmed ali"), Rec(1, "crm", "b", "ahmed ali") };
        var overrides = new StewardOverrides();
        overrides.Add("dms", "a", GoldenAttribute.Phone, "7700000001", T0.AddDays(1), 5);
        overrides.Add("crm", "b", GoldenAttribute.Phone, "7700000002", T0.AddDays(2), 6);
        overrides.Add("crm", "b", GoldenAttribute.Phone, "7700000003", T0.AddDays(2), 7); // the same moment: the later row

        Assert.Equal([(GoldenAttribute.Phone, "7700000003")], overrides.For(members));
    }

    [Fact]
    public void An_unknown_attribute_or_an_empty_value_is_not_an_override()
    {
        var overrides = new StewardOverrides();
        overrides.Add("dms", "a", "favourite_colour", "red", T0, 1);
        overrides.Add("dms", "a", GoldenAttribute.Phone, " ", T0, 2);

        Assert.True(overrides.IsEmpty);
        Assert.Equal(0, overrides.Count);
    }
}
