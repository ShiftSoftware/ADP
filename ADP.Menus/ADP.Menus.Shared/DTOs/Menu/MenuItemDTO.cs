using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuItemDTO
{
    /// <summary>
    /// This is just for the UI to display the replacement item details.
    /// </summary>
    public MenuItemReplacementItemDTO ReplacementItem { get; set; }

    [Required]
    public long? ReplacementItemVehicleModelID { get; set; }

    [Required]
    public List<MenuItemPartDTO> Parts { get; set; } = new();

    public decimal? StandaloneAllowedTime { get; set; }

    public bool NotStoredInMenuItemsTable { get; set; }
}

public class MenuItemPartDTOValidator : AbstractValidator<MenuItemPartDTO>
{
    public MenuItemPartDTOValidator(bool hasStandaloneItems)
    {
        RuleFor(x => x.PartNumber).NotEmpty();
        RuleFor(x => x.PartFinalPrice).NotNull();

        RuleFor(x => x.PeriodicQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PeriodicQuantity.HasValue);

        RuleFor(x => x.StandaloneQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => hasStandaloneItems && x.StandaloneQuantity.HasValue);

        RuleFor(x => x.PartPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PartPrice.HasValue);

        RuleFor(x => x.PartPriceMarginPercentage)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PartPriceMarginPercentage.HasValue);

        RuleFor(x => x.CountryPrices).NotNull().NotEmpty();
        RuleForEach(x => x.CountryPrices)
            .ChildRules(x =>
            {
                x.RuleFor(s => s.CountryID).NotNull();
                x.RuleFor(s => s.PartPrice).GreaterThanOrEqualTo(0).When(s => s.PartPrice.HasValue);
                x.RuleFor(s => s.PartPriceMarginPercentage).GreaterThanOrEqualTo(0).When(s => s.PartPriceMarginPercentage.HasValue);
                x.RuleFor(s => s.PartFinalPrice).NotNull();
            });
    }
}

public class MenuItemDTOValidator : AbstractValidator<MenuItemDTO>
{
    public MenuItemDTOValidator(bool hasStandaloneItems)
    {
        RuleFor(x => x.ReplacementItemVehicleModelID).NotNull();
        RuleFor(x => x.Parts).NotNull().NotEmpty();

        RuleForEach(x => x.Parts).SetValidator(new MenuItemPartDTOValidator(hasStandaloneItems));

        RuleFor(x => x.Parts.Count)
            .Equal(1)
            .When(x => !(x.ReplacementItem?.AllowMultiplePartNumbers ?? false));

        RuleFor(x => x.Parts.Count)
            .GreaterThanOrEqualTo(1)
            .When(x => x.ReplacementItem?.AllowMultiplePartNumbers ?? false);

        RuleFor(x => x.StandaloneAllowedTime).NotNull().When(x => hasStandaloneItems);
    }
}
