using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs;

/// <summary>
/// Locale-keyed string map. Serializes as <c>{ "en": "...", "ar": "...", "ku": "..." }</c>.
/// Per Decision #5, every human-facing string in the schema is a <see cref="LocalizedString"/>.
/// </summary>
public class LocalizedString : Dictionary<string, string>
{
    public LocalizedString() { }

    public LocalizedString(IDictionary<string, string> source) : base(source) { }

    public static LocalizedString From(string locale, string value) => new() { [locale] = value };
}

public class LocalizedStringValidator : AbstractValidator<LocalizedString>
{
    public LocalizedStringValidator()
    {
        RuleFor(x => x).NotNull();
        RuleFor(x => x.Count).GreaterThan(0)
            .WithMessage("Localized string must contain at least one locale.");
        RuleForEach(x => x.Values).NotEmpty()
            .WithMessage("Localized string values cannot be empty.");
    }
}
