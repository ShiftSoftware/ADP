using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// The rule the case browser's record search keeps a candidate by when it searches through the record index
/// (<see cref="CaseBrowserController.SearchMatches"/>): the same values its SQL matches. Values are synthetic.
/// </summary>
public class CaseBrowserSearchRuleTests
{
    static readonly RealRecord Record = new(
        Idx: 0, SourceSystem: "src-a", SourceRecordId: "Rec1", RawName: "Synth Person ", NormName: "SYNTH PERSON",
        Phones: ["7000000001"], WeakPhones: ["700000002"], NationalId: null, Dob: null);

    static bool Whole(string term) => CaseBrowserController.SearchMatches(Record, term, new([.. term.Where(char.IsDigit)]), partial: false);
    static bool Part(string term) => CaseBrowserController.SearchMatches(Record, term, new([.. term.Where(char.IsDigit)]), partial: true);

    [Theory]
    [InlineData("synth person", true)]          // either name, any case, the stored trailing space ignored
    [InlineData("Synth Person", true)]
    [InlineData("synth", false)]                // whole values only
    [InlineData("7000000001", true)]            // the full number
    [InlineData("0700 000 0001", false)]        // the digits as given, not reformatted
    [InlineData("700000002", true)]             // a weak phone in full
    [InlineData("70000000", false)]
    [InlineData("Rec1", true)]                  // the record key, exactly
    [InlineData("rec1", false)]
    public void A_caller_without_partial_search_matches_whole_values(string term, bool expected) =>
        Assert.Equal(expected, Whole(term));

    [Theory]
    [InlineData("nth per", true)]
    [InlineData("SYNTH", true)]
    [InlineData("0000000", true)]               // part of a phone
    [InlineData("700000002", false)]            // weak phones are not searched by part
    [InlineData("Rec1", true)]
    [InlineData("Rec", false)]                  // the key still matches whole only
    public void A_partial_search_caller_matches_parts_of_names_and_phones(string term, bool expected) =>
        Assert.Equal(expected, Part(term));
}
