using FluentValidation;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

public class ClaimableItemContractValidator : AbstractValidator<ClaimableItemContractDTO>
{
    public const decimal MaximumPriceIncrement = 0.10m;

    public ClaimableItemContractValidator(IStringLocalizer<ClaimableItemsResource> loc)
    {
        RuleFor(x => x.VIN)
            .NotEmpty()
            .WithMessage(loc["This field is required"]);

        RuleFor(x => x.VIN)
            .Must(x => VINValidation.IsValidVIN(x))
            .WithMessage(loc["Invalid VIN"]);

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage(loc["This field is required"]);

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0)
            .WithMessage(loc["Mileage cannot be negative"]);

        RuleFor(x => x.Duration)
            .GreaterThan(0)
            .WithMessage(loc["Duration must be greater than zero"]);

        RuleFor(x => x.TransferRate)
            .GreaterThan(0)
            .WithMessage(loc["Transfer rate must be greater than zero"]);

        RuleFor(x => x.PriceIncrement)
            .InclusiveBetween(0, MaximumPriceIncrement)
            .WithMessage(loc["Price increment must be between 0% and 10%"]);

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage(loc["Select at least one menu"]);

        // A periodic service twice on one contract makes no sense; standalone services may repeat.
        RuleFor(x => x.Lines)
            .Must(lines => lines
                .Where(l => l.LineType == ClaimableItemContractLineType.Periodic)
                .GroupBy(l => l.MenuLineKey)
                .All(g => g.Count() == 1))
            .WithMessage(loc["A periodic menu can be added to a contract only once"]);

        RuleForEach(x => x.Lines).SetValidator(new ClaimableItemContractLineValidator(loc));
    }
}

public class ClaimableItemContractLineValidator : AbstractValidator<ClaimableItemContractLineDTO>
{
    public ClaimableItemContractLineValidator(IStringLocalizer<ClaimableItemsResource> loc)
    {
        RuleFor(x => x.MenuLineKey)
            .NotEmpty()
            .WithMessage(loc["This field is required"]);

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0)
            .WithMessage(loc["Price cannot be negative"]);

        RuleFor(x => x.Duration)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.LineType == ClaimableItemContractLineType.Periodic)
            .WithMessage(loc["Enter a duration for every periodic menu"]);

        RuleFor(x => x.DurationUnit)
            .NotNull()
            .When(x => x.LineType == ClaimableItemContractLineType.Periodic)
            .WithMessage(loc["Enter a duration for every periodic menu"]);
    }
}
