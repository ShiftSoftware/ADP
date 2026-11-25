using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyLaborRate;

public class WarrantyLaborRateDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    /// <summary>ISO 4217 code of the claim currency, USD included.</summary>
    public string Currency { get; set; } = default!;

    /// <summary>The dealer's rate per hour, in <see cref="Currency"/>.</summary>
    public decimal LaborRate { get; set; }

    /// <summary>The distributor's rate per hour, in USD.</summary>
    public decimal LaborRateDistributor { get; set; }

    public DateTime? EffectiveDate { get; set; }
}

public class WarrantyLaborRateValidator : AbstractValidator<WarrantyLaborRateDTO>
{
    public WarrantyLaborRateValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$")
            .WithMessage("Use the three-letter ISO 4217 code, in capital letters.");

        // The scale is the columns': a finer rate would be rounded after the totals were computed from it.
        RuleFor(x => x.LaborRate)
            .GreaterThan(0)
            .PrecisionScale(18, 2, true);

        RuleFor(x => x.LaborRateDistributor)
            .GreaterThan(0)
            .PrecisionScale(18, 2, true);

        RuleFor(x => x.EffectiveDate)
            .NotNull();
    }
}
