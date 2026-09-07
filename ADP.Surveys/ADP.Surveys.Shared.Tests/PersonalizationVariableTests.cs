using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

/// <summary>
/// Inline <c>|fallback</c> syntax and survey-declared <see cref="SurveyVariableDto"/>s —
/// the layers behind the instance snapshot.
///
/// The load-bearing fact in here is that an <see cref="SurveyVariableDto.Example"/> is
/// invisible on a live instance. Examples are throwaway test data ("Camry"); fallbacks
/// are copy a real customer may read. If the two ever swapped roles, a test value would
/// go out to a recipient — so the isolation is asserted from both directions.
/// </summary>
public class PersonalizationVariableTests
{
    private static SurveyDto SurveyWith(params SurveyVariableDto[] variables) => new()
    {
        DefaultLocale = "en",
        Locales = new List<string> { "en", "ar" },
        Variables = variables.ToList(),
    };

    /// <summary>A fully-declared variable: test-run example plus customer-facing fallback.</summary>
    private static SurveyVariableDto Variable(string name, string? example, string? fallback) =>
        new()
        {
            Name = name,
            Example = Localized(example),
            Fallback = Localized(fallback),
        };

    private static LocalizedString Localized(string? en, string? ar = null)
    {
        var value = new LocalizedString();
        if (en is not null) value["en"] = en;
        if (ar is not null) value["ar"] = ar;
        return value;
    }

    private static PersonalizationContext Live(SurveyDto? survey, params (string Key, string Value)[] snapshot) =>
        PersonalizationContext.ForInstance(
            snapshot.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal), survey);

    private static PersonalizationContext Sample(SurveyDto? survey, params (string Key, string Value)[] snapshot) =>
        PersonalizationContext.ForSample(
            survey, snapshot.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));

    private const string Token = "{{candidate.customerName}}";
    private const string TokenWithInline = "{{candidate.customerName|dear customer}}";

    // ─── The example must never reach a recipient ────────────────────────────

    [Fact]
    public void Live_IgnoresTheExample_Entirely()
    {
        // "Camry" is the shape of thing authors type as an example. On a real
        // instance it must be as if it were never declared.
        var survey = SurveyWith(Variable("candidate.customerName", example: "Camry", fallback: null));

        Assert.Equal(Token, PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
    }

    [Fact]
    public void Live_UsesTheFallback_NotTheExample()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: "Camry", fallback: "our valued customer"));

        Assert.Equal(
            "our valued customer",
            PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
    }

    [Fact]
    public void Sample_UsesTheExample_NotTheFallback()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: "Camry", fallback: "our valued customer"));

        Assert.Equal("Camry", PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
    }

    // ─── Full precedence, both modes ─────────────────────────────────────────

    [Fact]
    public void Live_SnapshotBeatsInline_WhichBeatsFallback()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: "Camry", fallback: "our valued customer"));

        Assert.Equal(
            "Aza",
            PersonalizationTokens.SubstituteString(
                TokenWithInline, Live(survey, ("candidate.customerName", "Aza")), "en"));

        // The sentence's own wording is more specific than the survey-wide one.
        Assert.Equal(
            "dear customer",
            PersonalizationTokens.SubstituteString(TokenWithInline, Live(survey), "en"));

        Assert.Equal(
            "our valued customer",
            PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
    }

    [Fact]
    public void Sample_ExampleBeatsSnapshotInlineAndFallback()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: "Camry", fallback: "our valued customer"));

        Assert.Equal(
            "Camry",
            PersonalizationTokens.SubstituteString(
                TokenWithInline, Sample(survey, ("candidate.customerName", "Aza")), "en"));
    }

    [Fact]
    public void Sample_WithNoExample_PreviewsTheLivePathExactly()
    {
        // Leaving the example blank is a documented way to see what a recipient with a
        // missing field gets. Sample and Live must agree, token for token.
        var survey = SurveyWith(Variable("candidate.customerName", example: null, fallback: "our valued customer"));

        foreach (var input in new[] { Token, TokenWithInline, "{{candidate.other}}" })
        {
            Assert.Equal(
                PersonalizationTokens.SubstituteString(input, Live(survey), "en"),
                PersonalizationTokens.SubstituteString(input, Sample(survey), "en"));
        }
    }

    [Fact]
    public void Sample_FallsToSnapshotThenInlineThenFallback_WhenNoExample()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: null, fallback: "our valued customer"));

        Assert.Equal(
            "Aza",
            PersonalizationTokens.SubstituteString(
                TokenWithInline, Sample(survey, ("candidate.customerName", "Aza")), "en"));
        Assert.Equal("dear customer", PersonalizationTokens.SubstituteString(TokenWithInline, Sample(survey), "en"));
        Assert.Equal("our valued customer", PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
    }

    [Fact]
    public void Live_EmptySnapshotValueIsTreatedAsMissing()
    {
        var survey = SurveyWith(Variable("candidate.customerName", example: null, fallback: "our valued customer"));

        Assert.Equal(
            "our valued customer",
            PersonalizationTokens.SubstituteString(Token, Live(survey, ("candidate.customerName", "")), "en"));
    }

    // ─── Inline fallback grammar ─────────────────────────────────────────────

    [Fact]
    public void InlineFallback_IsTrimmed_AndSurvivesAnEmptyContext()
    {
        var empty = new PersonalizationContext(null);

        // The empty-context fast path must not skip a string whose fallback alone
        // can resolve it.
        Assert.Equal(
            "Hello friend.",
            PersonalizationTokens.SubstituteString("Hello {{candidate.name |  friend  }}.", empty, "en"));
    }

    [Fact]
    public void InlineFallback_MayCarryNonLatinCopy()
    {
        Assert.Equal(
            "مرحباً عميلنا العزيز",
            PersonalizationTokens.SubstituteString(
                "مرحباً {{candidate.customerName|عميلنا العزيز}}", Live(null), "ar"));
    }

    [Fact]
    public void InlineFallback_DoesNotRunPastTheClosingBraces()
    {
        // A stray pipe must not let one token swallow the rest of the sentence.
        Assert.Equal(
            "a b, and then some.",
            PersonalizationTokens.SubstituteString("a {{x|b}}, and then some.", Live(null), "en"));
    }

    [Fact]
    public void EmptyInlineFallback_FallsThroughToVerbatim()
    {
        // `{{x|}}` says nothing, so the author still sees their token rather than a
        // silently blanked sentence.
        Assert.Equal("{{x|}}", PersonalizationTokens.SubstituteString("{{x|}}", Live(null), "en"));
    }

    [Fact]
    public void TokenWithNothingBehindIt_StaysVerbatim()
    {
        var survey = SurveyWith(Variable("candidate.other", "x", "y"));

        Assert.Equal(
            "{{candidate.typo}}",
            PersonalizationTokens.SubstituteString("{{candidate.typo}}", Live(survey), "en"));
        Assert.Equal(
            "{{candidate.typo}}",
            PersonalizationTokens.SubstituteString("{{candidate.typo}}", Sample(survey), "en"));
    }

    // ─── Localization of both declared values ────────────────────────────────

    [Fact]
    public void DeclaredValues_ResolvePerLocale()
    {
        var survey = SurveyWith(new SurveyVariableDto
        {
            Name = "candidate.customerName",
            Example = Localized("Camry", "كامري"),
            Fallback = Localized("our valued customer", "عميلنا العزيز"),
        });

        Assert.Equal("Camry", PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
        Assert.Equal("كامري", PersonalizationTokens.SubstituteString(Token, Sample(survey), "ar"));
        Assert.Equal("our valued customer", PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
        Assert.Equal("عميلنا العزيز", PersonalizationTokens.SubstituteString(Token, Live(survey), "ar"));
    }

    [Fact]
    public void DeclaredValues_FallBackToDefaultLocaleThenAnyLocale()
    {
        // Translated in en only — the Kurdish screen still beats raw braces.
        var enOnly = SurveyWith(Variable("candidate.customerName", null, "our valued customer"));
        Assert.Equal("our valued customer", PersonalizationTokens.SubstituteString(Token, Live(enOnly), "ku"));

        // Translated in ar only, while the survey's default locale is en — any filled
        // locale is still better than nothing.
        var arOnly = SurveyWith(new SurveyVariableDto
        {
            Name = "candidate.customerName",
            Fallback = Localized(null, "عميلنا"),
        });
        Assert.Equal("عميلنا", PersonalizationTokens.SubstituteString(Token, Live(arOnly), "ku"));
    }

    [Fact]
    public void Substitute_ResolvesEachLocaleAgainstItsOwnTranslation()
    {
        var survey = SurveyWith(new SurveyVariableDto
        {
            Name = "candidate.customerName",
            Fallback = Localized("valued customer", "عميلنا العزيز"),
        });
        survey.Screens.Add(new InlineScreenDto
        {
            Id = "s1",
            Title = new LocalizedString
            {
                ["en"] = "Hello {{candidate.customerName}}",
                ["ar"] = "مرحباً {{candidate.customerName}}",
            },
        });

        PersonalizationTokens.Substitute(survey, PersonalizationContext.ForInstance(null, survey));

        var screen = (InlineScreenDto)survey.Screens[0];
        Assert.Equal("Hello valued customer", screen.Title!["en"]);
        Assert.Equal("مرحباً عميلنا العزيز", screen.Title!["ar"]);
    }

    // ─── The declaration site itself is never rewritten ──────────────────────

    [Fact]
    public void Substitute_LeavesTheVariableDeclarationsUntouched()
    {
        var survey = SurveyWith(
            Variable("candidate.customerName", "Camry", "valued customer"),
            // A variable whose own text looks like a token must not be rewritten by
            // its neighbour — the declarations are the source, not a target.
            Variable("candidate.note", "literally {{candidate.customerName}}", "also {{candidate.customerName}}"));

        PersonalizationTokens.Substitute(survey, PersonalizationContext.ForSample(survey));

        Assert.Equal("Camry", survey.Variables[0].Example["en"]);
        Assert.Equal("valued customer", survey.Variables[0].Fallback["en"]);
        Assert.Equal("literally {{candidate.customerName}}", survey.Variables[1].Example["en"]);
        Assert.Equal("also {{candidate.customerName}}", survey.Variables[1].Fallback["en"]);
    }

    // ─── Indexing rules ──────────────────────────────────────────────────────

    [Fact]
    public void HalfFilledVariableRows_AreIgnored()
    {
        var survey = SurveyWith(
            new SurveyVariableDto { Name = "  ", Fallback = Localized("orphan") },
            new SurveyVariableDto { Name = "candidate.customerName" });

        // An author part-way through adding a row must not start blanking tokens.
        Assert.Equal(Token, PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
        Assert.Equal(Token, PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
    }

    [Fact]
    public void ExampleAndFallback_AreIndexedIndependently()
    {
        // Example only: visible in a test run, invisible live.
        var exampleOnly = SurveyWith(Variable("candidate.customerName", "Camry", null));
        Assert.Equal("Camry", PersonalizationTokens.SubstituteString(Token, Sample(exampleOnly), "en"));
        Assert.Equal(Token, PersonalizationTokens.SubstituteString(Token, Live(exampleOnly), "en"));

        // Fallback only: visible in both.
        var fallbackOnly = SurveyWith(Variable("candidate.customerName", null, "valued customer"));
        Assert.Equal("valued customer", PersonalizationTokens.SubstituteString(Token, Sample(fallbackOnly), "en"));
        Assert.Equal("valued customer", PersonalizationTokens.SubstituteString(Token, Live(fallbackOnly), "en"));
    }

    [Fact]
    public void VariableNames_AreTrimmed()
    {
        var survey = SurveyWith(Variable("  candidate.customerName  ", "Camry", "valued customer"));

        Assert.Equal("valued customer", PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
        Assert.Equal("Camry", PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
    }

    [Fact]
    public void DuplicateVariableNames_LastRowWins()
    {
        var survey = SurveyWith(
            Variable("candidate.customerName", "first", "first fallback"),
            Variable("candidate.customerName", "second", "second fallback"));

        Assert.Equal("second fallback", PersonalizationTokens.SubstituteString(Token, Live(survey), "en"));
        Assert.Equal("second", PersonalizationTokens.SubstituteString(Token, Sample(survey), "en"));
    }

    // ─── Author-facing helpers ───────────────────────────────────────────────

    [Fact]
    public void CollectTokenNames_FindsEveryTokenInTheSurveysCopy()
    {
        var survey = new SurveyDto
        {
            Title = new LocalizedString { ["en"] = "About your {{candidate.vehicleModel}}" },
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "s1",
                    Description = new LocalizedString { ["ar"] = "مرحباً {{candidate.customerName|صديقنا}}" },
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new TextQuestionDto
                        {
                            Id = "q1",
                            Title = new LocalizedString { ["en"] = "Rate {{candidate.vehicleModel}} at {{recipient.address}}" },
                        }),
                    },
                },
            },
        };

        var names = PersonalizationTokens.CollectTokenNames(survey);

        Assert.Contains("candidate.vehicleModel", names);
        Assert.Contains("candidate.customerName", names);
        Assert.Contains("recipient.address", names);
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void CollectTokenNames_SkipsTheVariableDeclarations()
    {
        var survey = SurveyWith(Variable("candidate.customerName", "see {{candidate.a}}", "see {{candidate.b}}"));
        Assert.Empty(PersonalizationTokens.CollectTokenNames(survey));
    }

    [Theory]
    [InlineData("candidate.customerName", true)]
    [InlineData("recipient.address", true)]
    [InlineData("templateId", true)]
    [InlineData("has-car", true)]
    [InlineData("  candidate.wip  ", true)]
    [InlineData("1candidate", false)]
    [InlineData("candidate name", false)]
    [InlineData("candidate|name", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTokenName_MatchesWhatTheSubstituterCouldResolve(string? name, bool expected) =>
        Assert.Equal(expected, PersonalizationTokens.IsValidTokenName(name));
}
