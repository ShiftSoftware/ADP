using FluentValidation;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuDTO : ShiftEntityViewAndUpsertDTO
{
    [MenuHashId]
    public override string? ID { get; set; }

    [Required]
    public string BasicModelCode { get => field; set => field = value.Trim(); } = default!;

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; }

    [VehicleModelHashId]
    [Required]
    public ShiftEntitySelectDTO VehicleModel { get; set; } = default!;
}

public class MenuDTOValidator : AbstractValidator<MenuDTO>
{
    public MenuDTOValidator()
    {
        RuleFor(x => x.BasicModelCode).NotEmpty();
        RuleFor(x => x.VehicleModel).NotEmpty();
    }
}
