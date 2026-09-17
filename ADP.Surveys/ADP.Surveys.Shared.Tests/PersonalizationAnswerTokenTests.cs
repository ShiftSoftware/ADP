using System.Reflection;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Integrity;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

/// <summary>
/// The server side of answer references and request-field substitution.
///
/// Two facts carry the design: <c>{{answers.*}}</c> tokens pass through the server
/// untouched under every mode and every fallback (only the renderer knows the answer),
/// and a request field never receives raw braces — a value it cannot fill becomes
/// empty, and a value it can fill is escaped for where it lands.
/// </summary>
public class PersonalizationAnswerTokenTests
{
    private static SurveyDto SurveyWith(params SurveyVariableDto[] variables) => new()
    {
        DefaultLocale = "en",
        Locales = new List<string> { "en", "ar" },
        Variables = variables.ToList(),
    };

    private static SurveyVariableDto Variable(string name, string? example, string? fallback) => new()
    {
        Name = name,
        Example = example is null ? null : LocalizedString.From("en", example),
        Fallback = fallback is null ? null : LocalizedString.From("en", fallback),
    };

    private static PersonalizationContext Live(SurveyDto? survey, params (string Key, string Value)[] snapshot) =>
        PersonalizationContext.ForInstance(
            snapshot.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal), survey);

    private static PersonalizationContext Sample(SurveyDto? survey, params (string Key, string Value)[] snapshot) =>
        PersonalizationContext.ForSample(
            survey, snapshot.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));

    // ─── Grammar helpers ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("answers.nps", true, "nps")]
    [InlineData("answers.brand.label", true, "brand")]
    [InlineData("answers.has-car", true, "has-car")]
    [InlineData("answers.", false, null)]
    [InlineData("answers", false, null)]
    [InlineData("candidate.answers.x", false, null)]
    [InlineData("recipient.address", false, null)]
    public void AnswerTokenHelpers_RecogniseTheNamespace(string name, bool isAnswer, string? questionId)
    {
        Assert.Equal(isAnswer, PersonalizationTokens.IsAnswerToken(name));
        Assert.Equal(questionId, PersonalizationTokens.AnswerTokenQuestionId(name));
    }

    // ─── The server never resolves an answer ─────────────────────────────────

    [Theory]
    [InlineData("{{answers.brand}}")]
    [InlineData("{{answers.brand.label}}")]
    [InlineData("{{answers.brand|no brand}}")]
    public void AnswerTokens_StayVerbatim_InLiveAndSampleMode_EvenWhenDeclared(string token)
    {
        // Example AND fallback declared for the very same name: neither may fire on
        // the server, in either mode — the renderer owns this token.
        var survey = SurveyWith(Variable("answers.brand", example: "Camry", fallback: "your car"));
        var input = $"You chose {token}.";

        Assert.Equal(input, PersonalizationTokens.SubstituteString(input, Live(survey), "en"));
        Assert.Equal(input, PersonalizationTokens.SubstituteString(input, Sample(survey), "en"));
    }

    [Fact]
    public void AnswerTokens_StayVerbatim_InRequestFields_WhileOtherTokensResolve()
    {
        var survey = SurveyWith();
        var source = new OptionsSourceDto
        {
            Url = "https://api.example/v1/{{candidate.region}}/models?brand={{answers.brand}}",
            QueryParams = new() { ["vin"] = "{{candidate.vin}}", ["pick"] = "{{answers.brand}}" },
            Headers = new() { ["X-Ref"] = "{{recipient.customerRef}}", ["X-Pick"] = "{{answers.brand.label}}" },
            Method = "POST",
            Body = """{"vin":"{{candidate.vin}}","brand":"{{answers.brand}}"}""",
        };

        PersonalizationTokens.SubstituteOptionsSource(source,
            Live(survey, ("candidate.region", "north east"), ("candidate.vin", "JT1&2"), ("recipient.customerRef", "C-1")));

        Assert.Equal("https://api.example/v1/north%20east/models?brand={{answers.brand}}", source.Url);
        Assert.Equal("JT1&2", source.QueryParams["vin"]);
        Assert.Equal("{{answers.brand}}", source.QueryParams["pick"]);
        Assert.Equal("C-1", source.Headers["X-Ref"]);
        Assert.Equal("{{answers.brand.label}}", source.Headers["X-Pick"]);
        Assert.Equal("""{"vin":"JT1&2","brand":"{{answers.brand}}"}""", source.Body);
    }

    // ─── Request fields: escaping per surface, blank when unresolvable ───────

    [Fact]
    public void RequestFields_EscapeForTheirSurface()
    {
        var context = Live(SurveyWith(), ("candidate.name", "A \"quoted\" name/with & symbols\nsecond line"));

        Assert.Equal(
            "A%20%22quoted%22%20name%2Fwith%20%26%20symbols%0Asecond%20line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.Url));
        Assert.Equal(
            "A \"quoted\" name/with & symbols\nsecond line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.QueryValue));
        Assert.Equal(
            "A \"quoted\" name/with & symbolssecond line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.HeaderValue));
        Assert.Equal(
            "A \\\"quoted\\\" name/with & symbols\\nsecond line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.JsonBody));
        Assert.Equal(
            "A%20%22quoted%22%20name%2Fwith%20%26%20symbols%0Asecond%20line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.FormBody));
        Assert.Equal(
            "A \"quoted\" name/with & symbols\nsecond line",
            PersonalizationTokens.SubstituteString("{{candidate.name}}", context, "en", TokenSurface.RawBody));
    }

    [Fact]
    public void JsonBody_EscapedValue_IsValidInsideAJsonStringLiteral()
    {
        var context = Live(SurveyWith(), ("candidate.note", "tab\there \"q\" back\\slash"));
        var body = PersonalizationTokens.SubstituteString(
            """{"note":"{{candidate.note}}"}""", context, "en", TokenSurface.JsonBody);

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        Assert.Equal("tab\there \"q\" back\\slash", doc.RootElement.GetProperty("note").GetString());
    }

    [Fact]
    public void RequestFields_BlankAnUnresolvableToken_CopyKeepsIt()
    {
        var context = Live(SurveyWith());

        Assert.Equal("Hi {{candidate.missing}}",
            PersonalizationTokens.SubstituteString("Hi {{candidate.missing}}", context, "en", TokenSurface.Copy));
        Assert.Equal("?x=&y=1",
            PersonalizationTokens.SubstituteString("?x={{candidate.missing}}&y=1", context, "en", TokenSurface.Url));
        Assert.Equal("",
            PersonalizationTokens.SubstituteString("{{candidate.missing}}", context, "en", TokenSurface.HeaderValue));
        Assert.Equal("""{"a":""}""",
            PersonalizationTokens.SubstituteString("""{"a":"{{candidate.missing}}"}""", context, "en", TokenSurface.JsonBody));
    }

    [Fact]
    public void RequestFields_UseInlineAndDeclaredFallbacks_ThenEscape()
    {
        var survey = SurveyWith(Variable("candidate.city", example: "Erbil", fallback: "all cities"));
        var live = Live(survey);

        // Inline fallback wins over the declared one; both are escaped for the URL.
        Assert.Equal("q=any%20city",
            PersonalizationTokens.SubstituteString("q={{candidate.city|any city}}", live, "en", TokenSurface.Url));
        Assert.Equal("q=all%20cities",
            PersonalizationTokens.SubstituteString("q={{candidate.city}}", live, "en", TokenSurface.Url));
        // Sample mode: the example leads, exactly as in copy.
        Assert.Equal("q=Erbil",
            PersonalizationTokens.SubstituteString("q={{candidate.city}}", Sample(survey), "en", TokenSurface.Url));
    }

    [Theory]
    [InlineData("application/json", TokenSurface.JsonBody)]
    [InlineData("application/json; charset=utf-8", TokenSurface.JsonBody)]
    [InlineData("application/vnd.api+json", TokenSurface.JsonBody)]
    [InlineData("application/x-www-form-urlencoded", TokenSurface.FormBody)]
    [InlineData("text/plain", TokenSurface.RawBody)]
    [InlineData(null, TokenSurface.RawBody)]
    public void BodySurface_FollowsTheMediaType(string? contentType, TokenSurface expected)
    {
        Assert.Equal(expected, PersonalizationTokens.BodySurface(contentType));
    }

    [Fact]
    public void OptionsSource_BodyDefaultsToJson_AndFormBodiesPercentEncode()
    {
        var context = Live(SurveyWith(), ("candidate.name", "a b\"c"));

        var json = new OptionsSourceDto { Url = "https://x.example/", Method = "POST", Body = """{"n":"{{candidate.name}}"}""" };
        PersonalizationTokens.SubstituteOptionsSource(json, context);
        Assert.Equal("application/json", json.EffectiveContentType);
        Assert.Equal("""{"n":"a b\"c"}""", json.Body);

        var form = new OptionsSourceDto
        {
            Url = "https://x.example/",
            Method = "POST",
            ContentType = "application/x-www-form-urlencoded",
            Body = "n={{candidate.name}}",
        };
        PersonalizationTokens.SubstituteOptionsSource(form, context);
        Assert.Equal("n=a%20b%22c", form.Body);
    }

    // ─── Whole-schema walk reaches the request fields ────────────────────────

    [Fact]
    public void Substitute_WalksIntoOptionsSources_AndLeavesOtherStringsAlone()
    {
        var survey = SurveyWith();
        var question = new DropdownQuestionDto
        {
            Id = "branch",
            Title = LocalizedString.From("en", "Branch for {{candidate.city}}"),
            OptionsSource = new OptionsSourceDto
            {
                Url = "https://api.example/branches/{{candidate.city}}",
                QueryParams = new() { ["ref"] = "{{recipient.customerRef}}" },
                ItemsPath = "{{candidate.city}}", // a dot-path, not a request field — never touched
            },
        };
        survey.Screens.Add(new InlineScreenDto { Id = "s1", Questions = { QuestionEntryDto.FromInline(question) } });

        PersonalizationTokens.Substitute(survey, Live(survey, ("candidate.city", "Erbil"), ("recipient.customerRef", "C-9")));

        Assert.Equal("Branch for Erbil", question.Title["en"]);
        Assert.Equal("https://api.example/branches/Erbil", question.OptionsSource!.Url);
        Assert.Equal("C-9", question.OptionsSource.QueryParams!["ref"]);
        Assert.Equal("{{candidate.city}}", question.OptionsSource.ItemsPath);
    }

    [Fact]
    public void CollectTokenNames_SeesRequestFields_AndAnswerReferencesFoldTheLabelSuffix()
    {
        var survey = SurveyWith();
        survey.Screens.Add(new InlineScreenDto
        {
            Id = "s1",
            Title = LocalizedString.From("en", "Hello {{candidate.name}}, you picked {{answers.brand.label}}"),
            Questions =
            {
                QuestionEntryDto.FromInline(new DropdownQuestionDto
                {
                    Id = "model",
                    Title = LocalizedString.From("en", "Model"),
                    OptionsSource = new OptionsSourceDto
                    {
                        Url = "https://api.example/models/{{answers.brand}}",
                        Headers = new() { ["X-Ref"] = "{{recipient.customerRef}}" },
                        Method = "POST",
                        Body = """{"region":"{{candidate.region}}","year":"{{answers.year}}"}""",
                    },
                }),
            },
        });

        var names = PersonalizationTokens.CollectTokenNames(survey);
        Assert.Equal(
            new[] { "answers.brand", "answers.brand.label", "answers.year", "candidate.name", "candidate.region", "recipient.customerRef" },
            names.OrderBy(x => x, StringComparer.Ordinal));

        Assert.Equal(new[] { "brand", "year" },
            PersonalizationTokens.CollectAnswerReferences(survey).OrderBy(x => x, StringComparer.Ordinal));
    }

    // ─── Publish-time integrity ──────────────────────────────────────────────

    [Fact]
    public void Integrity_AnswerTokenMustNameAnExistingQuestion()
    {
        var survey = new SurveyDto
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
                    Questions = { QuestionEntryDto.FromInline(new TextQuestionDto { Id = "name", Title = LocalizedString.From("en", "Name") }) },
                },
                new InlineScreenDto
                {
                    Id = "s2",
                    Title = LocalizedString.From("en", "Thanks {{answers.name}} — {{answers.nmae}}"),
                    Questions =
                    {
                        QuestionEntryDto.FromInline(new DropdownQuestionDto
                        {
                            Id = "city",
                            Title = LocalizedString.From("en", "City"),
                            OptionsSource = new OptionsSourceDto { Url = "https://api.example/cities?for={{answers.reigon}}" },
                        }),
                    },
                },
            },
        };

        var errors = SurveyIntegrityValidator.Validate(survey);

        Assert.Contains(errors, e => e.Path == "tokens.answers.nmae" && e.Message.Contains("'nmae'"));
        Assert.Contains(errors, e => e.Path == "tokens.answers.reigon");
        Assert.DoesNotContain(errors, e => e.Path == "tokens.answers.name");
    }

    // ─── OptionsSource validation ────────────────────────────────────────────

    [Theory]
    [InlineData(null, true)]
    [InlineData("GET", true)]
    [InlineData("get", true)]
    [InlineData("POST", true)]
    [InlineData("post", true)]
    [InlineData("PUT", false)]
    [InlineData("DELETE", false)]
    public void OptionsSourceValidator_AllowsGetAndPostOnly(string? method, bool valid)
    {
        var source = new OptionsSourceDto { Url = "https://api.example/x", Method = method };
        Assert.Equal(valid, new OptionsSourceDtoValidator().Validate(source).IsValid);
    }

    [Fact]
    public void OptionsSourceValidator_BodyRequiresPost()
    {
        var source = new OptionsSourceDto { Url = "https://api.example/x", Body = "{}" };
        var result = new OptionsSourceDtoValidator().Validate(source);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("needs method POST"));

        source.Method = "POST";
        Assert.True(new OptionsSourceDtoValidator().Validate(source).IsValid);
    }

    [Fact]
    public void OptionsSourceValidator_AcceptsTokensInsideTheUrl()
    {
        var source = new OptionsSourceDto { Url = "https://api.example/v1/{{answers.region}}/branches?vin={{candidate.vin}}" };
        Assert.True(new OptionsSourceDtoValidator().Validate(source).IsValid);
    }

    // ─── Parity guard with the SDK's typed walk ──────────────────────────────

    /// <summary>
    /// The renderer substitutes answer tokens with a typed walk over the same
    /// localized fields, listed by hand in <c>survey-sdk/src/personalization.ts</c>
    /// (<c>LOCALIZED_KEYS</c>). This pins the set of LocalizedString-typed properties
    /// across the schema DTOs so a new localized field fails here until that list is
    /// updated too.
    /// </summary>
    [Fact]
    public void LocalizedStringFields_MatchTheSdkWalkList()
    {
        var expected = new[]
        {
            "description", "help", "highLabel", "label", "lowLabel", "noLabel",
            "placeholder", "title", "unit", "yesLabel",
            // survey.variables — the declaration site the walker never enters.
            "example", "fallback",
        };

        var actual = typeof(SurveyDto).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("ShiftSoftware.ADP.Surveys.Shared.DTOs", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.PropertyType == typeof(LocalizedString) || p.PropertyType == typeof(Dictionary<string, LocalizedString>))
            .Select(p => p.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name ?? p.Name)
            .Distinct()
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // optionLabels lives on the (unresolved) override DTO only — resolved away
        // at publish, so the renderer never meets it.
        Assert.Equal(expected.OrderBy(x => x, StringComparer.Ordinal), actual.Where(n => n != "optionLabels"));
    }
}
