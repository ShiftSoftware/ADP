using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;

/// <summary>
/// Catalog of demo surveys seeded into the sample API on startup. Each entry is intended
/// to illustrate one or two features of the schema so the builder list view shows a
/// concrete example for every advanced capability. Drafts only — open in the builder to
/// review and publish.
/// </summary>
public static class SampleSurveys
{
    public static IReadOnlyList<SampleSurveyRecipe> All { get; } = new[]
    {
        MinimalNps(),
        BranchingNavigation(),
        AllQuestionTypes(),
        MultiLocaleBranding(),
        ScreenTemplatesAndBank(),
        TriggerDriven(),
        Sf4PurchaseFollowUp.Recipe(),
    };

    // ──────────────────────────────────────────────────────────────────────
    // 1. Minimal — single NPS question followed by an explicit thanks screen.
    //    Smallest "happy path" survey; the explicit terminal screen is what
    //    gives the renderer a clean ending instead of falling off the flow.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe MinimalNps() => new(
        IntegrationId: "sample-minimal-nps",
        Name: "Sample: Minimal NPS",
        Draft: new SurveyDto
        {
            Title = LocalizedString.From("en", "Customer NPS"),
            Description = LocalizedString.From("en", "Tell us how we did in one question."),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "score",
                    Title = LocalizedString.From("en", "How likely are you to recommend us?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NpsQuestionDto
                        {
                            Id = "nps",
                            Title = LocalizedString.From("en", "0 = not at all, 10 = very likely"),
                            Required = true,
                            Min = 0,
                            Max = 10,
                            LowLabel = LocalizedString.From("en", "Not likely"),
                            HighLabel = LocalizedString.From("en", "Very likely"),
                            BiColumn = "nps_score",
                        })
                    },
                    NextScreen = "thanks",
                },
                new InlineScreenDto
                {
                    Id = "thanks",
                    Title = LocalizedString.From("en", "Thanks for your feedback!"),
                    Description = LocalizedString.From("en", "Your response has been recorded."),
                }
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());

    // ──────────────────────────────────────────────────────────────────────
    // 2. Branching — exercises both ways flow can be steered:
    //    • Cross-screen logic rules on the score screen fan into the standard
    //      NPS buckets: detractor (≤6), passive (7–8 via composite AND),
    //      promoter (9–10, falls through to the department screen).
    //    • The department screen uses a navigationList whose options each
    //      declare their own nextScreen (per-option transitions).
    //
    //    The detractor rule deliberately depends only on `score` so it fires
    //    on Next from the score screen; rules that depend on a navigationList
    //    answer never get a chance to run because navigationList options apply
    //    their own nextScreen the moment they're tapped.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe BranchingNavigation() => new(
        IntegrationId: "sample-branching-navigation",
        Name: "Sample: Branching navigation + logic",
        Draft: new SurveyDto
        {
            Title = LocalizedString.From("en", "Service feedback"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "score",
                    Title = LocalizedString.From("en", "How was your service visit?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NpsQuestionDto
                        {
                            Id = "score",
                            Title = LocalizedString.From("en", "Score us 0–10"),
                            Required = true,
                        }),
                    },
                    // Default for promoters (9–10) — detractor and passive logic intercepts
                    // before this when they match.
                    NextScreen = "department",
                },
                new InlineScreenDto
                {
                    Id = "department",
                    Title = LocalizedString.From("en", "Which department helped you?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "department",
                            Title = LocalizedString.From("en", "Pick one"),
                            Required = true,
                            Options =
                            {
                                new() { Id = "sales",   Label = LocalizedString.From("en", "Sales"),   NextScreen = "thanks-sales" },
                                new() { Id = "service", Label = LocalizedString.From("en", "Service"), NextScreen = "thanks-service" },
                                new() { Id = "parts",   Label = LocalizedString.From("en", "Parts"),   NextScreen = "thanks-generic" },
                            }
                        })
                    }
                },
                new InlineScreenDto { Id = "thanks-sales",   Title = LocalizedString.From("en", "Thanks — sales team will follow up.") },
                new InlineScreenDto { Id = "thanks-service", Title = LocalizedString.From("en", "Thanks — service manager will reach out.") },
                new InlineScreenDto { Id = "thanks-generic", Title = LocalizedString.From("en", "Thanks for your feedback!") },
                new InlineScreenDto
                {
                    Id = "detractor",
                    Title = LocalizedString.From("en", "Sorry to hear that. We'll be in touch."),
                    Description = LocalizedString.From("en", "A customer-care agent will reach out within 24h."),
                },
                new InlineScreenDto
                {
                    Id = "passive-followup",
                    Title = LocalizedString.From("en", "Thanks — what could have made it better?"),
                    Description = LocalizedString.From("en", "A passive bucket: there's room to improve."),
                },
            },
            // Rules are evaluated in declaration order on Next; first match wins.
            Logic =
            {
                // Detractor — single predicate, the most common branching shape.
                new LogicRuleDto
                {
                    If = new PredicateConditionDto
                    {
                        QuestionId = "score",
                        Op = LogicOperator.LessThanOrEqual,
                        Value = JsonSerializer.SerializeToElement(6),
                    },
                    Then = new LogicActionDto { Goto = "detractor" }
                },
                // Passive — composite AND to demonstrate the shape (7 ≤ score ≤ 8).
                new LogicRuleDto
                {
                    If = new CompositeConditionDto
                    {
                        All = new()
                        {
                            new PredicateConditionDto
                            {
                                QuestionId = "score",
                                Op = LogicOperator.GreaterThanOrEqual,
                                Value = JsonSerializer.SerializeToElement(7),
                            },
                            new PredicateConditionDto
                            {
                                QuestionId = "score",
                                Op = LogicOperator.LessThanOrEqual,
                                Value = JsonSerializer.SerializeToElement(8),
                            },
                        }
                    },
                    Then = new LogicActionDto { Goto = "passive-followup" }
                }
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());

    // ──────────────────────────────────────────────────────────────────────
    // 3. All question types — one example of every QuestionType, mostly two
    //    per screen so the builder list is browseable. Includes help text,
    //    validation bounds, options, and placeholders where applicable.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe AllQuestionTypes() => new(
        IntegrationId: "sample-all-question-types",
        Name: "Sample: All question types showcase",
        Draft: new SurveyDto
        {
            Title = LocalizedString.From("en", "Question type showcase"),
            Description = LocalizedString.From("en", "One screen per question type with the common knobs exercised."),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "text",
                    Title = LocalizedString.From("en", "Text & paragraph"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new TextQuestionDto
                        {
                            Id = "full-name",
                            Title = LocalizedString.From("en", "Your full name"),
                            Help = LocalizedString.From("en", "As it appears on your driving licence."),
                            Required = true,
                            MinLength = 2,
                            MaxLength = 80,
                            Placeholder = LocalizedString.From("en", "e.g. Jane Doe"),
                        }),
                        QuestionEntryDto.FromInline(new ParagraphQuestionDto
                        {
                            Id = "comments",
                            Title = LocalizedString.From("en", "Anything else?"),
                            MaxLength = 1000,
                            Placeholder = LocalizedString.From("en", "Optional notes…"),
                        }),
                    },
                    NextScreen = "numbers",
                },
                new InlineScreenDto
                {
                    Id = "numbers",
                    Title = LocalizedString.From("en", "Number, rating, NPS"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NumberQuestionDto
                        {
                            Id = "mileage",
                            Title = LocalizedString.From("en", "Current mileage"),
                            Required = true,
                            Min = 0,
                            Max = 1_000_000,
                            Step = 1,
                            Unit = LocalizedString.From("en", "km"),
                        }),
                        QuestionEntryDto.FromInline(new RatingQuestionDto
                        {
                            Id = "facility",
                            Title = LocalizedString.From("en", "Rate the facility"),
                            Max = 5,
                            AllowHalf = true,
                        }),
                        QuestionEntryDto.FromInline(new NpsQuestionDto
                        {
                            Id = "nps",
                            Title = LocalizedString.From("en", "Would you recommend us?"),
                            Required = true,
                        }),
                    },
                    NextScreen = "choices",
                },
                new InlineScreenDto
                {
                    Id = "choices",
                    Title = LocalizedString.From("en", "Choice questions"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new SingleChoiceQuestionDto
                        {
                            Id = "preferred-contact",
                            Title = LocalizedString.From("en", "How should we contact you?"),
                            Required = true,
                            Options =
                            {
                                new() { Id = "phone", Label = LocalizedString.From("en", "Phone") },
                                new() { Id = "email", Label = LocalizedString.From("en", "Email") },
                                new() { Id = "sms",   Label = LocalizedString.From("en", "SMS") },
                            }
                        }),
                        QuestionEntryDto.FromInline(new MultiChoiceQuestionDto
                        {
                            Id = "services-used",
                            Title = LocalizedString.From("en", "Which services did you use today?"),
                            MinSelected = 1,
                            MaxSelected = 3,
                            Options =
                            {
                                new() { Id = "oil",    Label = LocalizedString.From("en", "Oil change") },
                                new() { Id = "tyres",  Label = LocalizedString.From("en", "Tyre rotation") },
                                new() { Id = "wash",   Label = LocalizedString.From("en", "Wash & vacuum") },
                                new() { Id = "diag",   Label = LocalizedString.From("en", "Diagnostics") },
                            }
                        }),
                        QuestionEntryDto.FromInline(new DropdownQuestionDto
                        {
                            Id = "branch",
                            Title = LocalizedString.From("en", "Which branch did you visit?"),
                            Placeholder = LocalizedString.From("en", "Select a branch…"),
                            Options =
                            {
                                new() { Id = "ebl", Label = LocalizedString.From("en", "Erbil") },
                                new() { Id = "sul", Label = LocalizedString.From("en", "Sulaymaniyah") },
                                new() { Id = "duh", Label = LocalizedString.From("en", "Duhok") },
                            }
                        }),
                    },
                    NextScreen = "dates",
                },
                new InlineScreenDto
                {
                    Id = "dates",
                    Title = LocalizedString.From("en", "Dates & times"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new DateQuestionDto
                        {
                            Id = "next-visit",
                            Title = LocalizedString.From("en", "When would you like to come back?"),
                            MinDate = "2026-01-01",
                            MaxDate = "2027-12-31",
                        }),
                        QuestionEntryDto.FromInline(new DateTimeQuestionDto
                        {
                            Id = "pickup",
                            Title = LocalizedString.From("en", "Earliest pickup date & time"),
                            MinDateTime = "2026-01-01T08:00:00+03:00",
                        }),
                    },
                    NextScreen = "media",
                },
                new InlineScreenDto
                {
                    Id = "media",
                    Title = LocalizedString.From("en", "File & signature"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new FileQuestionDto
                        {
                            Id = "photo",
                            Title = LocalizedString.From("en", "Upload a photo of the issue"),
                            AcceptedTypes = new() { "image/png", "image/jpeg" },
                            MaxSizeBytes = 5L * 1024 * 1024,
                        }),
                        QuestionEntryDto.FromInline(new SignatureQuestionDto
                        {
                            Id = "signoff",
                            Title = LocalizedString.From("en", "Sign to confirm the work was completed"),
                            Required = true,
                        }),
                    },
                    NextScreen = "yesno-nav",
                },
                new InlineScreenDto
                {
                    Id = "yesno-nav",
                    Title = LocalizedString.From("en", "Yes / No & navigation list"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new YesNoQuestionDto
                        {
                            Id = "callback",
                            Title = LocalizedString.From("en", "Would you like a callback?"),
                            YesLabel = LocalizedString.From("en", "Yes please"),
                            NoLabel = LocalizedString.From("en", "No thanks"),
                        }),
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "next-step",
                            Title = LocalizedString.From("en", "What would you like to do next?"),
                            Required = true,
                            Options =
                            {
                                new() { Id = "book",   Label = LocalizedString.From("en", "Book a service"),     NextScreen = "thanks" },
                                new() { Id = "quote",  Label = LocalizedString.From("en", "Get a parts quote"),  NextScreen = "thanks" },
                                new() { Id = "none",   Label = LocalizedString.From("en", "Just finishing up"),  NextScreen = "thanks" },
                            }
                        }),
                    }
                },
                new InlineScreenDto { Id = "thanks", Title = LocalizedString.From("en", "Thanks for completing the survey!") },
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());

    // ──────────────────────────────────────────────────────────────────────
    // 4. Multi-locale + branding — en, ar, ku side-by-side with a brand palette
    //    and logo URL. Default locale is en; ar is RTL and should render that way
    //    in the previewer.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe MultiLocaleBranding() => new(
        IntegrationId: "sample-multi-locale-branding",
        Name: "Sample: Multi-locale + branding",
        Draft: new SurveyDto
        {
            Title = new LocalizedString
            {
                ["en"] = "Dealer satisfaction",
                ["ar"] = "رضا الوكيل",
                ["ku"] = "Rezamendiya nûnertiyê",
            },
            Description = new LocalizedString
            {
                ["en"] = "A short, multilingual feedback form.",
                ["ar"] = "نموذج ملاحظات قصير ومتعدد اللغات.",
                ["ku"] = "Formeke kurt û pirzimanî ji bo nêrînê.",
            },
            Locales = new() { "en", "ar", "ku" },
            DefaultLocale = "en",
            Branding = new BrandingDto
            {
                PrimaryColor = "#0055A4",
                SecondaryColor = "#F4A300",
                LogoUrl = "https://example.invalid/logo.png",
                FaviconUrl = "https://example.invalid/favicon.ico",
            },
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "intro",
                    Title = new LocalizedString
                    {
                        ["en"] = "Tell us about your visit",
                        ["ar"] = "أخبرنا عن زيارتك",
                        ["ku"] = "Li ser serdana xwe ji me re bibêje",
                    },
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new SingleChoiceQuestionDto
                        {
                            Id = "purpose",
                            Title = new LocalizedString
                            {
                                ["en"] = "Why did you visit?",
                                ["ar"] = "ما سبب زيارتك؟",
                                ["ku"] = "Çima hatî?",
                            },
                            Required = true,
                            Options =
                            {
                                new()
                                {
                                    Id = "buy",
                                    Label = new LocalizedString { ["en"] = "Buying a car", ["ar"] = "شراء سيارة",     ["ku"] = "Kirîna otomobîlê" },
                                },
                                new()
                                {
                                    Id = "service",
                                    Label = new LocalizedString { ["en"] = "Service",       ["ar"] = "صيانة",          ["ku"] = "Servîs" },
                                },
                                new()
                                {
                                    Id = "parts",
                                    Label = new LocalizedString { ["en"] = "Parts",         ["ar"] = "قطع غيار",       ["ku"] = "Parçeyên yedek" },
                                },
                            }
                        })
                    },
                    NextScreen = "thanks",
                },
                new InlineScreenDto
                {
                    Id = "thanks",
                    Title = new LocalizedString
                    {
                        ["en"] = "Thank you!",
                        ["ar"] = "شكراً لك!",
                        ["ku"] = "Spas dikim!",
                    },
                }
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());

    // ──────────────────────────────────────────────────────────────────────
    // 5. Screen template + banked question — demonstrates reuse. The survey
    //    references a banked NPS question and a screen template (customer-info)
    //    that itself composes two banked questions. Both bank entries and the
    //    template are seeded as prerequisites.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe ScreenTemplatesAndBank()
    {
        var fullNameBank = new BankRecipe(
            Key: "bank-full-name",
            Question: new TextQuestionDto
            {
                Id = "bank-full-name",
                Title = LocalizedString.From("en", "Full name"),
                Required = true,
                MinLength = 2,
                MaxLength = 120,
            },
            BiColumn: "full_name",
            Tags: "identity");

        var phoneBank = new BankRecipe(
            Key: "bank-phone",
            Question: new TextQuestionDto
            {
                Id = "bank-phone",
                Title = LocalizedString.From("en", "Phone number"),
                Required = true,
                Pattern = @"^\+?[0-9 ()-]{7,20}$",
                Placeholder = LocalizedString.From("en", "+964 …"),
            },
            BiColumn: "phone",
            Tags: "identity,contact");

        var npsBank = new BankRecipe(
            Key: "bank-nps",
            Question: new NpsQuestionDto
            {
                Id = "bank-nps",
                Title = LocalizedString.From("en", "How likely are you to recommend us? (0–10)"),
                Required = true,
            },
            BiColumn: "nps_global");

        var customerInfoTemplate = new TemplateRecipe(
            Key: "tmpl-customer-info",
            Template: new ScreenTemplateDto
            {
                Id = "tmpl-customer-info",
                Title = LocalizedString.From("en", "Customer details"),
                Description = LocalizedString.From("en", "Reused contact-info screen."),
                Questions =
                {
                    new QuestionRefDto { BankRef = fullNameBank.Key },
                    new QuestionRefDto { BankRef = phoneBank.Key },
                }
            },
            Tags: "identity");

        return new SampleSurveyRecipe(
            IntegrationId: "sample-bank-and-templates",
            Name: "Sample: Banked questions + screen template",
            Draft: new SurveyDto
            {
                Title = LocalizedString.From("en", "Post-purchase follow-up"),
                Locales = new() { "en" },
                DefaultLocale = "en",
                Screens =
                {
                    new ScreenTemplateRefDto
                    {
                        TemplateRef = customerInfoTemplate.Key,
                        Overrides = new ScreenTemplateOverridesDto
                        {
                            Title = LocalizedString.From("en", "Confirm your details"),
                            NextScreen = "score",
                            Questions = new()
                            {
                                [phoneBank.Key] = new QuestionOverridesDto { Required = false },
                            }
                        }
                    },
                    new InlineScreenDto
                    {
                        Id = "score",
                        Title = LocalizedString.From("en", "Recommend us?"),
                        Questions =
                        {
                            // Reference the banked NPS with a presentation override.
                            QuestionEntryDto.FromRef(new QuestionRefDto
                            {
                                BankRef = npsBank.Key,
                                Overrides = new QuestionOverridesDto
                                {
                                    Title = LocalizedString.From("en", "After buying from us, would you recommend us?"),
                                    Required = true,
                                }
                            })
                        }
                    }
                }
            },
            Banks: new[] { fullNameBank, phoneBank, npsBank },
            Templates: new[] { customerInfoTemplate });
    }

    // ──────────────────────────────────────────────────────────────────────
    // 6. Trigger-driven — survey is delivered automatically when an upstream
    //    `service-visit-closed` event fires for a non-warranty visit. Carries
    //    a filter (composite AND), a dedup recipe so the same WIP only triggers
    //    once per dealer, a schedule with an initial delay + two reminders, and
    //    is delivered via the in-memory channel registered by default.
    // ──────────────────────────────────────────────────────────────────────
    private static SampleSurveyRecipe TriggerDriven() => new(
        IntegrationId: "sample-trigger-driven",
        Name: "Sample: Trigger-driven service visit",
        Draft: new SurveyDto
        {
            Title = LocalizedString.From("en", "Service visit follow-up"),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                new InlineScreenDto
                {
                    Id = "score",
                    Title = LocalizedString.From("en", "How was your service visit?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NpsQuestionDto
                        {
                            Id = "service-nps",
                            Title = LocalizedString.From("en", "Rate your visit 0–10"),
                            Required = true,
                        })
                    },
                    NextScreen = "thanks",
                },
                new InlineScreenDto { Id = "thanks", Title = LocalizedString.From("en", "Thanks!") },
            },
            Triggers =
            {
                new TriggerDto
                {
                    Id = "service-visit-closed",
                    Enabled = true,
                    EventKind = "service-visit-closed",
                    Channel = "memory:default",
                    DedupRecipe = new() { "candidate.dealerId", "candidate.wip" },
                    Schedule = new TriggerScheduleDto
                    {
                        InitialDelay = "2h",
                        Reminders = new() { "3d", "7d" },
                    },
                    // Composite AND: only fire for non-warranty visits with a closed WIP.
                    Filter = new CompositeConditionDto
                    {
                        All = new()
                        {
                            new PredicateConditionDto
                            {
                                QuestionId = "candidate.warranty",
                                Op = LogicOperator.Equals,
                                Value = JsonSerializer.SerializeToElement(false),
                            },
                            new PredicateConditionDto
                            {
                                QuestionId = "candidate.wip",
                                Op = LogicOperator.IsSet,
                            },
                        }
                    }
                }
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());
}
