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

    /// <summary>
    /// Field-level cascade: the survey's own branding wins per field, the deployment
    /// default fills the gaps. Returns null when neither side has anything — callers
    /// can then skip the overlay entirely and serve the frozen schema bytes untouched.
    /// </summary>
    public static BrandingDto? Merge(BrandingDto? deploymentDefault, BrandingDto? survey)
    {
        if (deploymentDefault is null) return survey;
        if (survey is null) return deploymentDefault;

        return new BrandingDto
        {
            PrimaryColor = survey.PrimaryColor ?? deploymentDefault.PrimaryColor,
            SecondaryColor = survey.SecondaryColor ?? deploymentDefault.SecondaryColor,
            LogoUrl = survey.LogoUrl ?? deploymentDefault.LogoUrl,
            FaviconUrl = survey.FaviconUrl ?? deploymentDefault.FaviconUrl,
        };
    }
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
