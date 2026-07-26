using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

/// <summary>
/// Cell formatting for the response export. The formula cases are the important ones:
/// every value in this export originated from an anonymous respondent, and it lands in an
/// analyst's spreadsheet.
/// </summary>
public class SurveyCsvTests
{
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+1", "'+1")]
    [InlineData("-1", "'-1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    public void FormulaLeadingCharacters_AreNeutralised(string answer, string expected)
    {
        Assert.Equal(expected, SurveyCsv.EscapeCell(answer));
    }

    [Fact]
    public void FormulaThatAlsoNeedsQuoting_GetsBoth()
    {
        // The classic injection payload contains commas, so the two rules have to compose:
        // apostrophe first, then RFC 4180 quoting around the whole thing.
        var escaped = SurveyCsv.EscapeCell("=HYPERLINK(\"http://x\",\"click\")");

        Assert.StartsWith("\"'=HYPERLINK(", escaped);
        Assert.EndsWith("\"", escaped);
        Assert.Contains("\"\"", escaped);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void OrdinaryValues_PassThroughUnchanged(string? cell, string expected)
    {
        Assert.Equal(expected, SurveyCsv.EscapeCell(cell));
    }

    [Fact]
    public void CommasQuotesAndNewlines_AreQuotedPerRfc4180()
    {
        Assert.Equal("\"a,b\"", SurveyCsv.EscapeCell("a,b"));
        Assert.Equal("\"say \"\"hi\"\"\"", SurveyCsv.EscapeCell("say \"hi\""));
        Assert.Equal("\"line1\nline2\"", SurveyCsv.EscapeCell("line1\nline2"));
    }

    [Theory]
    [InlineData("\"text answer\"", "text answer")]
    [InlineData("9", "9")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", "")]
    public void ScalarAnswers_RenderAsBareValues(string json, string expected)
    {
        Assert.Equal(expected, SurveyCsv.RenderValue(json));
    }

    [Fact]
    public void MultiChoiceArray_RendersAsDelimitedList()
    {
        Assert.Equal("a | b | c", SurveyCsv.RenderValue("[\"a\",\"b\",\"c\"]"));
    }

    [Fact]
    public void ObjectAnswer_KeepsItsJson()
    {
        // File answers are objects. Flattening them to a placeholder would silently lose
        // the only record of what was attached.
        var rendered = SurveyCsv.RenderValue("{\"name\":\"scan.pdf\",\"size\":12}");

        Assert.Contains("scan.pdf", rendered);
    }

    [Fact]
    public void MalformedJson_IsPassedThroughRatherThanDropped()
    {
        Assert.Equal("not json at all", SurveyCsv.RenderValue("not json at all"));
    }

    [Fact]
    public void EmptyValue_RendersEmpty()
    {
        Assert.Equal("", SurveyCsv.RenderValue(null));
        Assert.Equal("", SurveyCsv.RenderValue("   "));
    }
}
