using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs;

/// <summary>
/// One personalization variable an author declares on a survey — the place both of
/// a token's stand-in values are configured.
///
/// The two are kept apart because they are answerable to different audiences and a
/// good value for one is usually a bad value for the other. An <see cref="Example"/>
/// is throwaway test data ("Camry", "Aza") that makes a test run look real and must
/// never reach a customer; a <see cref="Fallback"/> is production copy ("our valued
/// customer") that a real recipient may actually read. Collapsing them would force a
/// choice between test runs that look fake and a fallback that leaks test data.
///
/// When one sentence needs different wording from the survey-wide fallback, the
/// inline form <c>{{candidate.customerName|our valued customer}}</c> overrides it for
/// that occurrence — see <see cref="PersonalizationTokens"/> for the full precedence.
/// </summary>
public class SurveyVariableDto
{
    /// <summary>
    /// Token path, written exactly as it appears between the braces —
    /// <c>recipient.address</c>, <c>candidate.customerName</c>. Matches the
    /// trigger filter / dedup-recipe grammar so authors learn one set of names.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Stand-in used by dashboard test instances and the builder preview, where there
    /// is no ingested event to read from. Never used on a real instance, so it is free
    /// to be concrete, recognisable test data.
    /// <para>
    /// Leaving it empty is a legitimate choice, not an omission: the test run then
    /// falls through to whatever a real recipient with a missing field would get,
    /// which is how an author previews the fallback path.
    /// </para>
    /// </summary>
    [JsonPropertyName("example")]
    public LocalizedString Example { get; set; } = new();

    /// <summary>
    /// Customer-facing copy used on a <b>real</b> instance whose event payload lacks
    /// this field. This is the value that stops <c>{{candidate.customerName}}</c>
    /// reaching a recipient, so it has to read as something a stranger could be
    /// addressed by.
    /// <para>
    /// Empty means "no safety net": the token renders verbatim if the payload misses
    /// it. Sometimes correct — a field the event always carries — which is why this is
    /// surfaced as a caution in the builder rather than blocked at publish.
    /// </para>
    /// </summary>
    [JsonPropertyName("fallback")]
    public LocalizedString Fallback { get; set; } = new();

    /// <summary>
    /// Optional author-facing note about where the real value comes from
    /// (e.g. "set by the post-service event"). Never shown to a respondent.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Draft-time shape check. Deliberately permissive about both values being empty —
/// an author who adds a row and names it before filling anything in must be able to
/// save. The name, once typed, still has to be a token path the substituter could
/// actually match, otherwise the row is silently inert.
/// </summary>
public class SurveyVariableDtoValidator : AbstractValidator<SurveyVariableDto>
{
    public SurveyVariableDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(PersonalizationTokens.IsValidTokenName)
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Variable name must be a token path such as 'candidate.customerName' or 'recipient.address'.");

        When(x => x.Example is { Count: > 0 }, () =>
            RuleFor(x => x.Example).SetValidator(new LocalizedStringValidator()));

        When(x => x.Fallback is { Count: > 0 }, () =>
            RuleFor(x => x.Fallback).SetValidator(new LocalizedStringValidator()));
    }
}
