using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

namespace ShiftSoftware.ADP.Surveys.Sample.API.Data.Seed;

/// <summary>
/// "SF4" — six-month post-purchase follow-up, transcribed from the client's WhatsApp
/// flow design. The conversational menu structure maps onto <c>navigationList</c>
/// questions (each option records the answer AND navigates), free-text prompts onto
/// <c>paragraph</c> questions, and the bot's send/reminder cadence onto the trigger
/// schedule.
///
/// Deliberate translations from the source flow:
/// <list type="bullet">
/// <item>Personalization tokens ([Customer Name], [Vehicle Model], [Dealer Name]) are
/// rendered as generic copy for now — serve-time token substitution from instance
/// metadata is a candidate enhancement, not a schema feature today.</item>
/// <item>"The flow of Quotation / Service / Spare part will be initiated" boxes are
/// internal handoffs in the bot. Here each becomes an acknowledgement screen; the
/// real-world handoff is whatever subscribes to the response outbox event.</item>
/// <item>The bottom sticky notes (wrong-input message, 1-hour idle nudge, per-reminder
/// copy) are channel/bot mechanics. Reminder cadence lands in <see cref="TriggerScheduleDto"/>;
/// the message templates belong to the channel adapter, not the schema.</item>
/// </list>
/// </summary>
internal static class Sf4PurchaseFollowUp
{
    public static SampleSurveyRecipe Recipe() => new(
        IntegrationId: "sf4-purchase-followup",
        Name: "SF4 — Six-Month Purchase Follow-up",
        Draft: new SurveyDto
        {
            Title = LocalizedString.From("en", "Six-month follow-up"),
            Description = LocalizedString.From("en", "Maintain customer engagement by following up six months after purchase."),
            Locales = new() { "en" },
            DefaultLocale = "en",
            Screens =
            {
                // ── Greeting splash. Zero-question screens need an explicit NextScreen
                //    or the renderer treats them as terminal.
                new InlineScreenDto
                {
                    Id = "welcome",
                    Title = LocalizedString.From("en", "Warm greetings from Toyota 😊"),
                    Description = LocalizedString.From("en",
                        "Dear valued customer, being part of the Toyota family means a lot to us. " +
                        "This is your Toyota dealer team — we hope you are satisfied with your vehicle " +
                        "and that it has been serving you well over the past six months. " +
                        "We'd love to hear your experience so far! It only takes a minute!"),
                    NextScreen = "experience",
                },

                // ── Experience rating. Per-option routing: Poor detours through the
                //    recovery branch before rejoining the main menu.
                new InlineScreenDto
                {
                    Id = "experience",
                    Title = LocalizedString.From("en", "How is your experience with your Toyota?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "experience",
                            Title = LocalizedString.From("en", "Select one"),
                            Required = true,
                            BiColumn = "sf4_experience",
                            Options =
                            {
                                new() { Id = "excellent", Label = LocalizedString.From("en", "Excellent"), NextScreen = "main-menu" },
                                new() { Id = "good",      Label = LocalizedString.From("en", "Good"),      NextScreen = "main-menu" },
                                new() { Id = "poor",      Label = LocalizedString.From("en", "Poor"),      NextScreen = "poor-details" },
                            }
                        })
                    }
                },

                // ── Poor branch: capture details, acknowledge, rejoin the main menu.
                new InlineScreenDto
                {
                    Id = "poor-details",
                    Title = LocalizedString.From("en", "We're truly sorry to hear about your experience."),
                    Description = LocalizedString.From("en", "Your feedback matters to us, and we'll do our best to make it right."),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new ParagraphQuestionDto
                        {
                            Id = "poor-details",
                            Title = LocalizedString.From("en", "Kindly share the details so we can help right away."),
                            Required = true,
                            BiColumn = "sf4_poor_details",
                            Placeholder = LocalizedString.From("en", "Tell us what went wrong…"),
                        })
                    },
                    NextScreen = "poor-ack",
                },
                new InlineScreenDto
                {
                    Id = "poor-ack",
                    Title = LocalizedString.From("en", "Thanks for letting us know!"),
                    Description = LocalizedString.From("en", "Our team will follow up with you shortly."),
                    NextScreen = "main-menu",
                },

                // ── Main menu — the heart of the flow. Six options, six destinations.
                new InlineScreenDto
                {
                    Id = "main-menu",
                    Title = LocalizedString.From("en", "Thank you so much for your valued feedback! 🙏"),
                    Description = LocalizedString.From("en", "We would be happy to assist you further. Please select one of the options below for our team to support you."),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "followup-intent",
                            Title = LocalizedString.From("en", "Select an option"),
                            Required = true,
                            BiColumn = "sf4_intent",
                            Options =
                            {
                                new() { Id = "explore-models",    Label = LocalizedString.From("en", "Explore Our Car Models"),      NextScreen = "handoff-quotation" },
                                new() { Id = "book-service",      Label = LocalizedString.From("en", "Book a Service Appointment"),  NextScreen = "handoff-service" },
                                new() { Id = "parts-accessories", Label = LocalizedString.From("en", "Request Parts & Accessories"), NextScreen = "handoff-parts" },
                                new() { Id = "happy-with-toyota", Label = LocalizedString.From("en", "I'm Happy with My Toyota"),    NextScreen = "happy" },
                                new() { Id = "sold-vehicle",      Label = LocalizedString.From("en", "I've Sold My Vehicle"),        NextScreen = "sold-why" },
                                new() { Id = "end-chat",          Label = LocalizedString.From("en", "End Chat"),                    NextScreen = "final-thanks" },
                            }
                        })
                    }
                },

                // ── Handoff acknowledgements (menu options 1–3). The downstream flows
                //    (quotation / service / spare parts) are initiated by whatever
                //    subscribes to the outbox event carrying the recorded intent.
                new InlineScreenDto
                {
                    Id = "handoff-quotation",
                    Title = LocalizedString.From("en", "Our sales team will take it from here 🚗"),
                    Description = LocalizedString.From("en", "The quotation flow will be initiated — expect our latest models and offers shortly."),
                    NextScreen = "final-thanks",
                },
                new InlineScreenDto
                {
                    Id = "handoff-service",
                    Title = LocalizedString.From("en", "Service appointment request received"),
                    Description = LocalizedString.From("en", "The service flow will be initiated — our team will contact you to schedule your appointment at the nearest dealer branch."),
                    NextScreen = "final-thanks",
                },
                new InlineScreenDto
                {
                    Id = "handoff-parts",
                    Title = LocalizedString.From("en", "Parts & accessories request received"),
                    Description = LocalizedString.From("en", "The spare-parts flow will be initiated — our team will reach out with availability and pricing."),
                    NextScreen = "final-thanks",
                },

                // ── Option 4: happy customer → nudge toward a service booking.
                new InlineScreenDto
                {
                    Id = "happy",
                    Title = LocalizedString.From("en", "We are pleased to know that you are satisfied with your vehicle 🙏"),
                    Description = LocalizedString.From("en",
                        "Your satisfaction is our priority. Thank you for being part of our journey and for choosing Toyota. " +
                        "To keep your vehicle in top condition, kindly schedule your upcoming appointment at the nearest dealer branch!"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "happy-next",
                            Title = LocalizedString.From("en", "Would you like to book a service for this car?"),
                            Required = true,
                            BiColumn = "sf4_happy_next",
                            Options =
                            {
                                new() { Id = "request-service", Label = LocalizedString.From("en", "Request a Service Appointment"), NextScreen = "handoff-service" },
                                new() { Id = "main-menu",       Label = LocalizedString.From("en", "Return to main menu"),           NextScreen = "main-menu" },
                                new() { Id = "end-chat",        Label = LocalizedString.From("en", "End Chat"),                      NextScreen = "final-thanks" },
                            }
                        })
                    }
                },

                // ── Option 5: sold the vehicle → optional reason, then a re-engagement menu.
                new InlineScreenDto
                {
                    Id = "sold-why",
                    Title = LocalizedString.From("en", "If you'd like, could you tell us why you decided to sell your vehicle?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "sold-share",
                            Title = LocalizedString.From("en", "Happy to listen either way"),
                            Required = true,
                            BiColumn = "sf4_sold_share",
                            Options =
                            {
                                new() { Id = "share", Label = LocalizedString.From("en", "Share Why I Sold"), NextScreen = "sold-input" },
                                new() { Id = "later", Label = LocalizedString.From("en", "I'll Share Later"), NextScreen = "sold-ack" },
                            }
                        })
                    }
                },
                new InlineScreenDto
                {
                    Id = "sold-input",
                    Title = LocalizedString.From("en", "Why did you decide to sell your vehicle?"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new ParagraphQuestionDto
                        {
                            Id = "sold-reason",
                            Title = LocalizedString.From("en", "Your feedback helps us build better ownership experiences."),
                            Required = true,
                            BiColumn = "sf4_sold_reason",
                            Placeholder = LocalizedString.From("en", "Share as much or as little as you like…"),
                        })
                    },
                    NextScreen = "sold-ack",
                },
                new InlineScreenDto
                {
                    Id = "sold-ack",
                    Title = LocalizedString.From("en", "Thank you for sharing your feedback 🙏"),
                    Description = LocalizedString.From("en", "Since you've driven Toyota and know the experience, we thought you might like to check our current models."),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new NavigationListQuestionDto
                        {
                            Id = "sold-next",
                            Title = LocalizedString.From("en", "What would you like to do next?"),
                            Required = true,
                            BiColumn = "sf4_sold_next",
                            Options =
                            {
                                new() { Id = "explore-models", Label = LocalizedString.From("en", "Explore Our Car Models"), NextScreen = "handoff-quotation" },
                                new() { Id = "main-menu",      Label = LocalizedString.From("en", "Return to main menu"),    NextScreen = "main-menu" },
                                new() { Id = "end-chat",       Label = LocalizedString.From("en", "End Chat"),               NextScreen = "final-thanks" },
                            }
                        })
                    }
                },

                // ── Terminal screen — zero questions, no NextScreen → renderer
                //    auto-submits on arrival and shows this content as the done state.
                new InlineScreenDto
                {
                    Id = "final-thanks",
                    Title = LocalizedString.From("en", "Thank you for your time!"),
                    Description = LocalizedString.From("en",
                        "We wish you a wonderful day 👋 If you're thinking about upgrading, trade in your " +
                        "current vehicle with Toyota and get the best value! Thank you for being part of the " +
                        "Toyota family. We truly value your trust and look forward to serving you again. " +
                        "DRIVE WITH CONFIDENCE 🚗"),
                },
            },
            Triggers =
            {
                // "Follow up after 6 months (SF4) from purchased vehicle date." The host
                // app ingests a `vehicle-purchased` event at sale time; the 180-day initial
                // delay lands the first send at the six-month mark. Reminder cadence per
                // the design's notes: +24h, then +3d, then +7d.
                new TriggerDto
                {
                    Id = "sf4-six-month-followup",
                    Enabled = true,
                    EventKind = "vehicle-purchased",
                    Channel = "memory:default",
                    DedupRecipe = new() { "candidate.dealerId", "candidate.vin" },
                    Schedule = new TriggerScheduleDto
                    {
                        InitialDelay = "180d",
                        Reminders = new() { "1d", "3d", "7d" },
                    },
                }
            }
        },
        Banks: Array.Empty<BankRecipe>(),
        Templates: Array.Empty<TemplateRecipe>());
}
