using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuDetailReportQueryDTO
{
    public DateTimeOffset? OldVersionDateTime { get; set; }

    public DateTimeOffset? NewVersionDateTime { get; set; }

    [Required]
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public List<ShiftEntitySelectDTO> Brands { get; set; } = default!;

    [Required]
    public long? CountryID { get; set; }

    [Required]
    public decimal? TransferRate { get; set; }
}

public class MenuDetailReportQueryDTOValidator : AbstractValidator<MenuDetailReportQueryDTO>
{
    public MenuDetailReportQueryDTOValidator()
    {
        // New version date time must be greater than old version date time
        RuleFor(x => x.NewVersionDateTime)
            .GreaterThan(x => x.OldVersionDateTime)
            .WithMessage("New version date must be greater than old version date.");

        // At least one brand must be selected
        RuleFor(x => x.Brands)
            .Must(brands => brands.Any())
            .WithMessage("At least one brand must be selected.");

        RuleFor(x => x.CountryID)
            .NotNull()
            .WithMessage("Country is required.");

        RuleFor(x => x.TransferRate)
            .NotNull()
            .GreaterThan(0);
    }
}
