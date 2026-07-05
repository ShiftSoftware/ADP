using FluentValidation;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;

// Moved into the module (was kept in TCA earlier because it localized via TCA's Resource). It now uses the
// module's own resource (IStringLocalizer<ClaimableItemsResource>) — the same "package owns its resource,
// consumer's AddLocalization resolves it" idiom ShiftBlazor uses.
public class CampaignVinEntryValidator : AbstractValidator<CampaignVinEntryDTO>
{
    public CampaignVinEntryValidator(IStringLocalizer<ClaimableItemsResource> loc)
    {
        RuleFor(x => x.VIN)
            .NotEmpty()
            .WithMessage(loc["This field is required"]);

        RuleFor(x => x.VIN)
            .Must(x => VINValidation.IsValidVIN(x))
            .When(x => !x.DisableVinValidation)
            .WithMessage(loc["Invalid VIN"]);
    }
}
