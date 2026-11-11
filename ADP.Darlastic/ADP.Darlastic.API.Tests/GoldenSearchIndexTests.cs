using ShiftSoftware.ADP.Darlastic.API.Controllers;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// The case browser's per-run snapshot of every golden's name and phone (<see cref="GoldenSearchIndex"/>): which
/// goldens a customer search names, by part of a value or by the whole of it. Values are synthetic.
/// </summary>
public class GoldenSearchIndexTests
{
    static readonly GoldenSearchIndex Index = GoldenSearchIndex.Build(
    [
        (10, "Synth Person", "7000000001"),
        (11, null, null),                       // nothing to find
        (12, "Other  Name ", "7000000002"),
        (13, "شخص تجريبي", "7000000001"),
        (14, "synth person", ""),
        (15, "Person", "7000000003"),
    ]);

    static long[] Sorted(HashSet<long> found) => [.. found.Order()];

    [Fact]
    public void Every_golden_is_in_the_snapshot()
    {
        Assert.Equal(6, Index.Count);
        Assert.True(Index.Bytes > 0);
    }

    [Fact]
    public void A_whole_name_matches_ignoring_case_and_trailing_spaces()
    {
        Assert.Equal([10, 14], Sorted(Index.ByName("SYNTH PERSON", partial: false)));
        Assert.Equal([12], Sorted(Index.ByName("other  name", partial: false)));
        Assert.Equal([13], Sorted(Index.ByName("شخص تجريبي", partial: false)));
        Assert.Equal([15], Sorted(Index.ByName("person", partial: false)));
        Assert.Empty(Index.ByName("synth", partial: false));
    }

    [Fact]
    public void Part_of_a_name_matches_only_when_asked()
    {
        Assert.Equal([10, 14, 15], Sorted(Index.ByName("erso", partial: true)));
        Assert.Equal([13], Sorted(Index.ByName("تجريبي", partial: true)));
        Assert.Equal([10, 14], Sorted(Index.ByName("NTH P", partial: true)));
        Assert.Empty(Index.ByName("nobody", partial: true));
    }

    [Fact]
    public void A_phone_matches_its_own_column_only()
    {
        Assert.Equal([10, 13], Sorted(Index.ByPhone("7000000001", partial: false)));
        Assert.Empty(Index.ByPhone("700000000", partial: false));
        Assert.Equal([10, 12, 13, 15], Sorted(Index.ByPhone("700000000", partial: true)));
        Assert.Equal([15], Sorted(Index.ByPhone("0003", partial: true)));
        Assert.Empty(Index.ByName("7000000001", partial: true));
        Assert.Empty(Index.ByPhone("Person", partial: true));
    }

    [Fact]
    public void An_empty_search_finds_nothing()
    {
        Assert.Empty(Index.ByName("", partial: true));
        Assert.Empty(Index.ByName("   ", partial: false));
        Assert.Empty(Index.ByPhone("", partial: true));
    }

    [Fact]
    public void A_search_cannot_join_two_goldens_values()
    {
        // The last letters of one golden's name and the first of the next are not a match.
        var index = GoldenSearchIndex.Build([(1, "AB", "12"), (2, "CD", "34")]);
        Assert.Empty(index.ByName("BC", partial: true));
        Assert.Empty(index.ByPhone("23", partial: true));
        Assert.Equal([1], Sorted(index.ByName("B", partial: true)));

        var zero = GoldenSearchIndex.Build([(1, "A\0B", null)]);
        Assert.Equal([1], Sorted(zero.ByName("a b", partial: false)));
        Assert.Empty(zero.ByName("a", partial: false));
    }

    [Fact]
    public void An_empty_registry_finds_nothing()
    {
        var empty = GoldenSearchIndex.Build([]);
        Assert.Equal(0, empty.Count);
        Assert.Empty(empty.ByName("x", partial: true));
        Assert.Empty(empty.ByPhone("1", partial: false));
    }
}
