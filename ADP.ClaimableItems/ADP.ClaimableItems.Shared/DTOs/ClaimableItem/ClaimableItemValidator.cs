using FluentValidation;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;

public class ClaimableItemValidator : AbstractValidator<ClaimableItemDTO>
{
    public ClaimableItemValidator(IStringLocalizer<ClaimableItemsResource> loc)
    {
        RuleFor(x => x.EligibilityConditions)
            .Must(EligibilityConditionsJson.IsValid)
            .WithMessage(loc["Enter leaf conditions or non-empty anyOf groups using supported fields and properties. Groups cannot nest; set whenUnmet on the group, not its alternatives."]);
    }
}
