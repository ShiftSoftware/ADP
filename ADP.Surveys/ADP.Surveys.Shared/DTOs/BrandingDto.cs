using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs;

public class BrandingDto
{
    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("secondaryColor")]
    public string? SecondaryColor { get; set; }

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("faviconUrl")]
    public string? FaviconUrl { get; set; }
}

public class BrandingDtoValidator : AbstractValidator<BrandingDto>
{
    private static readonly Regex HexColor = new(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

    public BrandingDtoValidator()
    {
        RuleFor(x => x.PrimaryColor).Must(BeHexColor!)
            .When(x => !string.IsNullOrEmpty(x.PrimaryColor))
            .WithMessage("primaryColor must be a hex color (#rgb, #rrggbb or #rrggbbaa).");
        RuleFor(x => x.SecondaryColor).Must(BeHexColor!)
            .When(x => !string.IsNullOrEmpty(x.SecondaryColor))
            .WithMessage("secondaryColor must be a hex color.");
    }

    private static bool BeHexColor(string value) => HexColor.IsMatch(value);
}
