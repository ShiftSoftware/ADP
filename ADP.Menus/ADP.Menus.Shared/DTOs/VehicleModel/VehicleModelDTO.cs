using FluentValidation;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourDetails;
using ShiftSoftware.ADP.Menus.Shared.DTOs.LabourRate;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class VehicleModelDTO : ShiftEntityViewAndUpsertDTO
{
    [VehicleModelHashId]
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public ShiftEntitySelectDTO Brand { get; set; } = default!;

    [Required]
    public decimal? LabourRate { get; set; }

    [Required]
    public List<LabourRateByCountryDTO> LabourRates { get; set; } = new();

    [Required]
    public List<LabourDetailsDTO> LabourDetails { get; set; }

    [Required]
    public List<VehicleModelDTOReplacementItem> ReplacementItems { get; set; }

    public VehicleModelDTO()
    {
        ReplacementItems = new List<VehicleModelDTOReplacementItem>();
    }
}

public class VehicleModelDTOValidator : AbstractValidator<VehicleModelDTO>
{
    public VehicleModelDTOValidator()
    {
        RuleForEach(x => x.LabourDetails)
            .ChildRules(x =>
            {
                x.RuleFor(x => x.AllowedTime).NotNull();
                x.RuleFor(x => x.Consumable).NotNull();
            });

        RuleForEach(x => x.LabourRates)
            .ChildRules(x =>
            {
                x.RuleFor(x => x.LabourRate).NotNull();
            });
    }
}
