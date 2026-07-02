using ShiftSoftware.ADP.Surveys.Shared.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Resolver;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class SchemaResolverTests
{
    [Fact]
    public void Resolve_UnknownBankRef_ReturnsError()
    {
        var draft = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "screen-1",
                    Questions = { QuestionEntryDto.FromRef(new QuestionRefDto { BankRef = "missing" }) }
                }
            }
        };

        var result = SchemaResolver.Resolve(draft, new InMemoryBankSource());

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Message.Contains("'missing'"));
    }

    [Fact]
    public void Resolve_BankRef_ExpandsInlineAndPreservesBankedId()
    {
        var bank = new BankQuestionDto
        {
            Id = "full-name",
            Question = new TextQuestionDto
            {
                Id = "full-name",
                Title = LocalizedString.From("en", "Full Name"),
                MinLength = 1,
            }
        };
        var source = new InMemoryBankSource(new[] { bank });

        var draft = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "screen-1",
                    Questions =
                    {
                        QuestionEntryDto.FromRef(new QuestionRefDto
                        {
                            BankRef = "full-name",
                            Overrides = new QuestionOverridesDto
                            {
                                Title = LocalizedString.From("en", "Your full name"),
                                Required = true,
                            }
                        })
                    }
                }
            }
        };

        var result = SchemaResolver.Resolve(draft, source);

        Assert.True(result.Success);
        var screen = (InlineScreenDto)result.Survey!.Screens[0];
        var text = Assert.IsType<TextQuestionDto>(screen.Questions[0].Inline);
        Assert.Equal("full-name", text.Id);              // banked id preserved
        Assert.Equal("Your full name", text.Title["en"]); // override applied
        Assert.True(text.Required);                      // override applied
        Assert.Equal(1, text.MinLength);                 // validation preserved
    }

    [Fact]
    public void Resolve_TemplateRef_ExpandsIntoInlineScreenWithBankedQuestions()
    {
        var nameBank = new BankQuestionDto
        {
            Id = "full-name",
            Question = new TextQuestionDto { Id = "full-name", Title = LocalizedString.From("en", "Full Name") }
        };
        var genderBank = new BankQuestionDto
        {
            Id = "gender",
            Question = new SingleChoiceQuestionDto
            {
                Id = "gender",
                Title = LocalizedString.From("en", "Gender"),
                Options = { new() { Id = "male", Label = LocalizedString.From("en", "Male") },
                            new() { Id = "female", Label = LocalizedString.From("en", "Female") } }
            }
        };
        var template = new ScreenTemplateDto
        {
            Id = "customer-info",
            Title = LocalizedString.From("en", "Customer Info"),
            Questions =
            {
                new QuestionRefDto { BankRef = "full-name" },
                new QuestionRefDto { BankRef = "gender" },
            }
        };
        var source = new InMemoryBankSource(
            questions: new[] { nameBank, genderBank },
            templates: new[] { template });

        var draft = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new ScreenTemplateRefDto
                {
                    TemplateRef = "customer-info",
                    Overrides = new ScreenTemplateOverridesDto
                    {
                        Title = LocalizedString.From("en", "Tell us about yourself"),
                        Questions = new()
                        {
                            ["gender"] = new QuestionOverridesDto { Required = false }
                        }
                    }
                }
            }
        };

        var result = SchemaResolver.Resolve(draft, source);

        Assert.True(result.Success);
        var screen = Assert.IsType<InlineScreenDto>(result.Survey!.Screens[0]);
        Assert.Equal("customer-info", screen.Id);
        Assert.Equal("Tell us about yourself", screen.Title!["en"]);
        Assert.Equal(2, screen.Questions.Count);
        Assert.Equal("full-name", screen.Questions[0].Inline!.Id);
        var gender = Assert.IsType<SingleChoiceQuestionDto>(screen.Questions[1].Inline);
        Assert.False(gender.Required);                   // override applied
        Assert.Equal(2, gender.Options.Count);           // validation/options preserved
    }

    [Fact]
    public void Resolve_OverridingBankQuestionDoesNotMutateBank()
    {
        var bank = new BankQuestionDto
        {
            Id = "q",
            Question = new TextQuestionDto { Id = "q", Title = LocalizedString.From("en", "Original") }
        };
        var source = new InMemoryBankSource(new[] { bank });

        var draft = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "s1",
                    Questions =
                    {
                        QuestionEntryDto.FromRef(new QuestionRefDto
                        {
                            BankRef = "q",
                            Overrides = new QuestionOverridesDto { Title = LocalizedString.From("en", "Overridden") }
                        })
                    }
                }
            }
        };

        SchemaResolver.Resolve(draft, source);

        Assert.Equal("Original", bank.Question.Title["en"]);
    }

    [Fact]
    public void Resolve_SourceParamsOverride_MergesOverBankQueryParams()
    {
        var bank = new BankQuestionDto
        {
            Id = "preferred-branch",
            Question = new DropdownQuestionDto
            {
                Id = "preferred-branch",
                Title = LocalizedString.From("en", "Preferred branch"),
                OptionsSource = new OptionsSourceDto
                {
                    Url = "https://example.test/api/public/company-branch",
                    QueryParams = new() { ["services"] = "new-vehicle-sale", ["top"] = "50" },
                },
            }
        };
        var source = new InMemoryBankSource(new[] { bank });

        var draft = new SurveyDto
        {
            SurveyId = "s",
            Title = LocalizedString.From("en", "S"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "screen-1",
                    Questions =
                    {
                        QuestionEntryDto.FromRef(new QuestionRefDto
                        {
                            BankRef = "preferred-branch",
                            Overrides = new QuestionOverridesDto
                            {
                                SourceParams = new() { ["services"] = "body-and-paint" },
                            }
                        })
                    }
                }
            }
        };

        var result = SchemaResolver.Resolve(draft, source);

        Assert.True(result.Success);
        var dd = Assert.IsType<DropdownQuestionDto>(((InlineScreenDto)result.Survey!.Screens[0]).Questions[0].Inline);
        Assert.NotNull(dd.OptionsSource);
        Assert.Equal("https://example.test/api/public/company-branch", dd.OptionsSource!.Url); // bank-locked
        Assert.Equal("body-and-paint", dd.OptionsSource.QueryParams!["services"]);             // override wins
        Assert.Equal("50", dd.OptionsSource.QueryParams["top"]);                               // bank param retained
        // The bank's own definition stays untouched (deep-cloned before overrides).
        Assert.Equal("new-vehicle-sale", ((DropdownQuestionDto)bank.Question).OptionsSource!.QueryParams!["services"]);
    }
}
