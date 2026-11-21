using FluentValidation;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyCurrencyRate;

public class WarrantyCurrencyRateDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    /// <summary>ISO 4217 code.</summary>
    public string Currency { get; set; } = default!;

    /// <summary>Units of <see cref="Currency"/> per 1 USD.</summary>
    public decimal Rate { get; set; }

    public DateTime? EffectiveDate { get; set; }
}

public class WarrantyCurrencyRateValidator : AbstractValidator<WarrantyCurrencyRateDTO>
{
    public WarrantyCurrencyRateValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$")
            .WithMessage("Use the three-letter ISO 4217 code, in capital letters.");

        RuleFor(x => x.Currency)
            .Must(x => !DocumentCurrency.IsUSD(x))
            .WithMessage("USD needs no rate.");

        RuleFor(x => x.Rate)
            .GreaterThan(0);

        RuleFor(x => x.EffectiveDate)
            .NotNull();
    }
}
