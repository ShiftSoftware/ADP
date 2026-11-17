using FluentValidation;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;

public class MenuVariantDTO : ShiftEntityViewAndUpsertDTO
{
    [MenuVariantHashId]
    public override string? ID { get; set; }

    [MenuHashId]
    [Required]
    public string MenuID { get; set; } = default!;

    [Required]
    public string Name { get => field; set => field = value.Trim(); } = default!;

    [Required]
    public string MenuPrefix { get => field; set => field = value.Trim(); } = default!;

    public string? MenuPostfix { get => field; set => field = !string.IsNullOrWhiteSpace(value) ? value.Trim() : null; }

    public string? StandaloneMenuPrefix { get => field; set => field = !string.IsNullOrWhiteSpace(value) ? value.Trim() : null; }

    public string? StandaloneMenuPostfix { get => field; set => field = !string.IsNullOrWhiteSpace(value) ? value.Trim() : null; }

    [Required]
    public decimal? LabourRate { get; set; }

    [Required]
    public List<LabourRateByCountryDTO> LabourRates { get; set; } = new();

    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// The types the variant's menu is offered under — any of paid, free, contract — at least one. A label only;
    /// it changes no price or code. With one type every period and standalone item belongs to it; with more,
    /// each lists the types it is visible under.
    /// </summary>
    public List<MenuType> MenuTypes { get; set; } = [MenuType.Paid];

    public bool HasStandaloneItems { get; set; }

    [Required]
    public List<LabourDetailsDTO> LabourDetails { get; set; } = new();

    [Required]
    public List<ServiceIntervalIDSelectorDTO> PeriodicAvailabilities { get; set; } = new();

    [Required]
    public List<MenuItemDTO> Items { get; set; } = new();
}

public class MenuVariantDTOValidator : AbstractValidator<MenuVariantDTO>
{
    public MenuVariantDTOValidator()
    {
        RuleFor(x => x.MenuID).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.MenuPrefix).NotEmpty();
        RuleFor(x => x.LabourRate).NotNull();
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.MenuTypes)
            .Must(x => MenuTypeSet.Normalize(x).Count > 0)
            .WithMessage("Select at least one menu type.");
        RuleForEach(x => x.Items).SetValidator(x => new MenuItemDTOValidator(x.HasStandaloneItems));
        RuleForEach(x => x.LabourRates)
            .ChildRules(x =>
            {
                x.RuleFor(x => x.LabourRate).NotNull();
            });
        RuleForEach(x => x.LabourDetails)
            .ChildRules(x =>
            {
                x.RuleFor(x => x.AllowedTime).NotNull();
                x.RuleFor(x => x.Consumable).NotNull();
            });
    }
}
