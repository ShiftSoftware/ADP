using ShiftSoftware.ADP.Darlastic.API.Controllers;
using Xunit;
using static ShiftSoftware.ADP.Darlastic.API.Controllers.RecordSearchIndex;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// The case browser's per-run record index (<see cref="RecordSearchIndex"/>): which staged records a search names as
/// candidates, in key order, and how a record index and a position map back to a record's key. Values are synthetic.
/// </summary>
public class RecordSearchIndexTests
{
    static readonly Row[] Rows =
    [
        new("src-a", "1", 10, "SYNTH PERSON", "Synth Person", ["7000000001"], []),
        new("src-a", "2", 11, "", "", [], []),                                   // nothing searchable but its key
        new("src-a", "3", 12, "OTHER NAME", "Other  Name ", ["7000000002", "7000000003"], ["700000004"]),
        new("src-b", "1", 13, "SYNTH PERSON", "شخص تجريبي", null, null),
        new("src-b", "a1", 14, null, "Mixed Case", ["7000000001"], []),
        new("src-b", "A1", 15, "UPPER KEY", "Upper Key", [], []),
    ];

    static readonly RecordSearchIndex Index = Build(Rows);

    static List<string> Keys(IEnumerable<int> positions) =>
        [.. positions.Select(p => Index.KeyAt(p)).Select(k => $"{k.SourceSystem}:{k.SourceRecordId}")];

    [Fact]
    public void Every_row_is_a_position_in_key_order()
    {
        Assert.Equal(Rows.Length, Index.Count);
        for (int i = 0; i < Rows.Length; i++)
            Assert.Equal((Rows[i].SourceSystem, Rows[i].SourceRecordId), Index.KeyAt(i));
    }

    [Fact]
    public void A_whole_name_matches_either_name_ignoring_case_and_trailing_spaces()
    {
        Assert.Equal(["src-a:1", "src-b:1"], Keys(Index.Find((Fields.Names, Fields.None, "synth person"))));
        Assert.Equal(["src-a:3"], Keys(Index.Find((Fields.Names, Fields.None, "OTHER  NAME"))));
        Assert.Equal(["src-b:1"], Keys(Index.Find((Fields.Names, Fields.None, "شخص تجريبي"))));
        Assert.Empty(Index.Find((Fields.Names, Fields.None, "synth")));
    }

    [Fact]
    public void Part_of_a_name_matches_by_fragment_only_when_asked()
    {
        Assert.Equal(["src-a:1", "src-b:1"], Keys(Index.Find((Fields.None, Fields.Names, "nth per"))));
        Assert.Equal(["src-b:1"], Keys(Index.Find((Fields.None, Fields.Names, "تجريبي"))));
        Assert.Equal(["src-b:a1"], Keys(Index.Find((Fields.None, Fields.Names, "ED CA"))));
    }

    [Fact]
    public void A_phone_matches_its_own_column_only()
    {
        Assert.Equal(["src-a:1", "src-b:a1"], Keys(Index.Find((Fields.Phones, Fields.None, "7000000001"))));
        Assert.Equal(["src-a:3"], Keys(Index.Find((Fields.Phones, Fields.None, "7000000003"))));
        Assert.Empty(Index.Find((Fields.Phones, Fields.None, "700000004")));
        Assert.Equal(["src-a:3"], Keys(Index.Find((Fields.WeakPhones, Fields.None, "700000004"))));
        Assert.Empty(Index.Find((Fields.Names, Fields.None, "7000000001")));
        Assert.Equal(["src-a:1", "src-a:3", "src-b:a1"], Keys(Index.Find((Fields.None, Fields.Phones, "000000"))));
    }

    [Fact]
    public void A_record_key_matches_whole_in_any_case_so_the_caller_decides_case()
    {
        Assert.Equal(["src-a:1", "src-b:1"], Keys(Index.Find((Fields.RecordIds, Fields.None, "1"))));
        Assert.Equal(["src-b:a1", "src-b:A1"], Keys(Index.Find((Fields.RecordIds, Fields.None, "a1"))));
        Assert.Empty(Index.Find((Fields.RecordIds, Fields.None, "src-a:1")));
    }

    [Fact]
    public void Several_searches_merge_into_one_ordered_list_without_repeats()
    {
        var found = Index.Find(
            (Fields.Names | Fields.RecordIds, Fields.None, "1"),
            (Fields.Phones | Fields.WeakPhones, Fields.None, "7000000001"),
            (Fields.Names, Fields.None, "upper key"));
        Assert.Equal(["src-a:1", "src-b:1", "src-b:a1", "src-b:A1"], Keys(found));
    }

    [Fact]
    public void An_empty_search_finds_nothing()
    {
        Assert.Empty(Index.Find((Fields.Names | Fields.Phones | Fields.RecordIds, Fields.Names, "")));
        Assert.Empty(Index.Find((Fields.Names, Fields.Names, "   ")));
        Assert.Empty(Index.Find((Fields.Names, Fields.Names, null)));
    }

    [Fact]
    public void A_record_index_maps_to_its_position()
    {
        Assert.Equal(0, Index.PositionOfIdx(10));
        Assert.Equal(5, Index.PositionOfIdx(15));
        Assert.Equal(-1, Index.PositionOfIdx(9));
        Assert.Equal(-1, Index.PositionOfIdx(-1));
    }

    [Fact]
    public void An_empty_registry_has_no_positions()
    {
        var empty = Build([]);
        Assert.Equal(0, empty.Count);
        Assert.Empty(empty.Find((Fields.Names, Fields.Names, "x")));
        Assert.Equal(-1, empty.PositionOfIdx(0));
    }

    [Fact]
    public void A_zero_character_in_a_value_cannot_join_two_values()
    {
        var index = Build([new("src-a", "1", 0, "A\0B", "", [], [])]);
        Assert.Single(index.Find((Fields.Names, Fields.None, "a b")));
        Assert.Empty(index.Find((Fields.Names, Fields.None, "a")));
    }
}
